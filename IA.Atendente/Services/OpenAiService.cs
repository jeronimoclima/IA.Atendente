using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using IA.Atendente.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Services;

public class OpenAiService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly AgendamentoService _agendamentoService;

    public OpenAiService(
        AppDbContext context,
        IConfiguration configuration,
        HttpClient httpClient,
        AgendamentoService agendamentoService)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
        _agendamentoService = agendamentoService;
    }

    public async Task<string> GerarRespostaAsync(Guid tenantId, string identificadorCliente, string mensagemUsuario)
    {
        // 1. Buscar a clínica, suas configurações de IA e serviços
        var tenant = await _context.Tenants
            .Include(t => t.AiConfiguration)
            .Include(t => t.Servicos)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null || tenant.AiConfiguration == null)
            throw new Exception("Clínica ou configuração de IA não encontrada.");

        // Buscar profissionais cadastrados no tenant
        var profissionais = await _context.Profissionais
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();

        // 2. Buscar ou criar a sessão de atendimento do cliente (ex: WhatsApp)
        var sessao = await _context.SessoesAtendimento
            .Include(s => s.Mensagens)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IdentificadorCliente == identificadorCliente);

        if (sessao == null)
        {
            sessao = new Domain.SessaoAtendimento
            {
                TenantId = tenantId,
                IdentificadorCliente = identificadorCliente
            };
            _context.SessoesAtendimento.Add(sessao);
        }

        // 3. Montar a Base de Conhecimento dos Serviços e Profissionais (Com suporte a sessões/minutos)
        var sbServicos = new StringBuilder();
        foreach (var servico in tenant.Servicos)
        {
            var detalheCobranca = servico.TipoCobranca == Domain.TipoCobranca.PorSessao
                ? $"{servico.QuantidadeSessoes} sessão(ões) - Duração de cada sessão: {servico.DuracaoMinutos} min"
                : $"Duração: {servico.DuracaoMinutos} min";

            sbServicos.AppendLine($"- ID: {servico.Id} | {servico.Descricao}: R$ {servico.Preco} ({detalheCobranca})");
        }

        var sbProfissionais = new StringBuilder();
        foreach (var prof in profissionais)
        {
            sbProfissionais.AppendLine($"- ID: {prof.Id} | Nome: {prof.Nome} ({prof.Especialidade})");
        }

        // 4. Construir o System Prompt dinâmico combinando o prompt + dados da clínica + serviços + profissionais + data atual
        var dataHoraAtual = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss (ddd)");
        var systemPromptCompleto = $"""
        {tenant.AiConfiguration.SystemPrompt}
        
        INFORMAÇÕES DE DATA E HORA DE HOJE:
        - Data/Hora Atual (UTC): {dataHoraAtual}
        
        INFORMAÇÕES GERAIS DA CLÍNICA:
        - Endereço / Localização: {tenant.Endereco}
        - Horário de Atendimento: {tenant.HorarioFuncionamento}
        
        PROFISSIONAIS DISPONÍVEIS:
        {sbProfissionais}

        CATÁLOGO DE SERVIÇOS DISPONÍVEIS NESTA CLÍNICA:
        {sbServicos}
        
        Regra: Responda sempre de forma cordial, humanizada e objetiva. Para agendamentos, você possui ferramentas para verificar disponibilidade e criar agendamentos. Quando o cliente quiser agendar, utilize as ferramentas (tools) fornecidas.
        """;

        // 5. Montar o histórico de mensagens para enviar à OpenAI
        var messagesList = new List<object>
        {
            new { role = "system", content = systemPromptCompleto }
        };

        // Adicionar histórico anterior para manter o contexto da conversa (limitado às últimas 20)
        var ultimasMensagens = sessao.Mensagens
            .OrderByDescending(m => m.CriadoEm)
            .Take(20)
            .OrderBy(m => m.CriadoEm);

        foreach (var msg in ultimasMensagens)
        {
            messagesList.Add(new { role = msg.Role, content = msg.Conteudo });
        }

        // Adicionar a mensagem atual do usuário
        messagesList.Add(new { role = "user", content = mensagemUsuario });

        // Salvar a mensagem do usuário no banco
        sessao.Mensagens.Add(new Domain.Mensagem { Role = "user", Conteudo = mensagemUsuario });
        sessao.AtualizadoEm = DateTime.UtcNow;

        // 6. Definição das Tools (Function Calling da OpenAI)
        var tools = new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "criar_agendamento",
                    description = "Cria um agendamento de consulta ou procedimento para o cliente.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            nomeCliente = new { type = "string", description = "Nome informado pelo cliente" },
                            profissionalId = new { type = "string", description = "ID do profissional selecionado (GUID)" },
                            servicoId = new { type = "string", description = "ID do serviço selecionado (GUID)" },
                            dataHoraInicio = new { type = "string", description = "Data e hora no formato ISO 'yyyy-MM-ddTHH:mm:ss'" }
                        },
                        required = new[] { "nomeCliente", "profissionalId", "servicoId", "dataHoraInicio" }
                    }
                }
            }
        };

        // 7. Configurar a requisição para a API da OpenAI
        var apiKey = _configuration["OpenAiSettings:ApiKey"];
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = tenant.AiConfiguration.ModeloIa,
            messages = messagesList,
            tools = tools,
            tool_choice = "auto",
            temperature = (float)tenant.AiConfiguration.Temperatura
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", jsonContent);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro na API da OpenAI: {errorContent}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseString);
        var choiceMessage = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message");

        string respostaIa = "";

        // 8. Verificar se a IA decidiu chamar a função de agendamento (Tool Call)
        if (choiceMessage.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.GetArrayLength() > 0)
        {
            var firstToolCall = toolCalls[0];
            var functionName = firstToolCall.GetProperty("function").GetProperty("name").GetString();
            var argumentsJson = firstToolCall.GetProperty("function").GetProperty("arguments").GetString();

            if (functionName == "criar_agendamento" && !string.IsNullOrEmpty(argumentsJson))
            {
                using var argsDoc = JsonDocument.Parse(argumentsJson);
                var root = argsDoc.RootElement;

                string nomeCliente = root.GetProperty("nomeCliente").GetString() ?? "Cliente";
                Guid profId = Guid.Parse(root.GetProperty("profissionalId").GetString()!);
                Guid servId = Guid.Parse(root.GetProperty("servicoId").GetString()!);
                DateTime dataHora = DateTime.Parse(root.GetProperty("dataHoraInicio").GetString()!);

                // Executa a validação e gravação no banco de dados
                var resultado = await _agendamentoService.CriarAgendamentoAsync(
                    tenantId,
                    identificadorCliente,
                    nomeCliente,
                    profId,
                    servId,
                    dataHora
                );

                if (resultado.Sucesso)
                {
                    respostaIa = $"✅ Agendamento realizado com sucesso para **{nomeCliente}**!\n\n📅 **Data/Hora:** {dataHora:dd/MM/yyyy HH:mm}\n📌 **Status:** Confirmado.";
                }
                else
                {
                    respostaIa = $"Não foi possível concluir o agendamento: {resultado.Mensagem}";
                }
            }
        }
        else
        {
            // Resposta normal por texto da IA
            respostaIa = choiceMessage.GetProperty("content").GetString() ?? "Desculpe, não consegui gerar uma resposta.";
        }

        // 9. Salvar a resposta da IA no banco
        sessao.Mensagens.Add(new Domain.Mensagem { Role = "assistant", Conteudo = respostaIa });
        await _context.SaveChangesAsync();

        return respostaIa;
    }
}