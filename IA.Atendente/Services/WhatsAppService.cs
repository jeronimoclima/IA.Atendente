// Services/WhatsAppService.cs
using System.Text;
using System.Text.Json;

namespace IA.Atendente.Api.Services;

public class WhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(HttpClient httpClient, IConfiguration configuration, ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarMensagemAsync(string remoteJid, string mensagem)
    {
        var baseUrl = _configuration["WhatsAppSettings:BaseUrl"];
        var apiKey = _configuration["WhatsAppSettings:ApiKey"];
        var instanceName = _configuration["WhatsAppSettings:InstanceName"];

        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(instanceName))
        {
            _logger.LogWarning("Configurações do WhatsApp não encontradas. O envio foi simulado.");
            return;
        }

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("apikey", apiKey);

        // Ajustado para a estrutura padrão da Evolution API v2.x (v2.3.7)
        var payload = new
        {
            number = remoteJid.Replace("@s.whatsapp.net", ""),
            text = mensagem // Mudou de 'textMessage = { text = ... }' para apenas 'text' na raiz
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var url = $"{baseUrl}/message/sendText/{instanceName}";

        var response = await _httpClient.PostAsync(url, jsonContent);
        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            _logger.LogError("Erro ao enviar mensagem via WhatsApp: {Erro}", erro);
            throw new Exception($"Erro ao enviar mensagem no WhatsApp: {erro}");
        }
    }
}