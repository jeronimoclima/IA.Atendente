// Services/OpenAiService.cs
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

    public OpenAiService(AppDbContext context, IConfiguration configuration, HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> GerarRespostaAsync(Guid tenantId, string identificadorCliente, string mensagemUsuario)
    {
        // 1. Buscar a clínica e suas configurações de IA
        var tenant = await _context.Tenants
            .Include(t => t.AiConfiguration)
            .Include(t => t.Servicos)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null || tenant.AiConfiguration == null)
            throw new Exception("Clínica ou configuração de IA não encontrada.");

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

        // 3. Montar a Base de Conhecimento dos Serviços (RAG dinâmico)
        var sbServicos = new StringBuilder();
        foreach (var servico in tenant.Servicos)
        {
            sbServicos.AppendLine($"- {servico.Descricao}: R$ {servico.Preco} (Duração: {servico.Duracao} min)");
        }

        // 4. Construir o System Prompt dinâmico combinando o prompt do banco + catálogo de serviços
        var systemPromptCompleto = $"""
        {tenant.AiConfiguration.SystemPrompt}
        
        CATÁLOGO DE SERVIÇOS DISPONÍVEIS NESTA CLÍNICA:
        {sbServicos.ToString()}
        
        Regra: Responda sempre de forma cordial, humanizada e objetiva, focando em tirar dúvidas e realizar agendamentos com base nos serviços acima.
        """;

        // 5. Montar o histórico de mensagens para enviar à OpenAI
        var messagesList = new List<object>
        {
            new { role = "system", content = systemPromptCompleto }
        };

        // Adicionar histórico anterior para manter o contexto da conversa
        foreach (var msg in sessao.Mensagens.OrderBy(m => m.CriadoEm))
        {
            messagesList.Add(new { role = msg.Role, content = msg.Conteudo });
        }

        // Adicionar a mensagem atual do usuário
        messagesList.Add(new { role = "user", content = mensagemUsuario });

        // Salvar a mensagem do usuário no banco
        sessao.Mensagens.Add(new Domain.Mensagem { Role = "user", Conteudo = mensagemUsuario });
        sessao.AtualizadoEm = DateTime.UtcNow;

        // 6. Configurar a requisição para a API da OpenAI
        var apiKey = _configuration["OpenAiSettings:ApiKey"];
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var requestBody = new
        {
            model = tenant.AiConfiguration.ModeloIa,
            messages = messagesList,
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
        var respostaIa = jsonDoc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Desculpe, não consegui gerar uma resposta.";

        // 7. Salvar a resposta da IA no banco (Usando 'Conteudo' com C maiúsculo)
        sessao.Mensagens.Add(new Domain.Mensagem { Role = "assistant", Conteudo = respostaIa });
        await _context.SaveChangesAsync();

        return respostaIa;
    }
}