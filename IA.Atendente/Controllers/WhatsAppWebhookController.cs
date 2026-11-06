// Controllers/WhatsAppWebhookController.cs
using IA.Atendente.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/webhook/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly OpenAiService _openAiService;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        OpenAiService openAiService,
        WhatsAppService whatsAppService,
        ILogger<WhatsAppWebhookController> logger)
    {
        _openAiService = openAiService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    [HttpPost("{tenantId:guid}")]
    public async Task<IActionResult> ReceberMensagem(Guid tenantId, [FromBody] JsonElement payload)
    {
        try
        {
            // 1. Valida se é um evento de mensagem da Evolution API (messages.upsert)
            if (!payload.TryGetProperty("event", out var eventProp) || eventProp.GetString() != "messages.upsert")
            {
                return Ok(new { status = "Ignored event" });
            }

            var data = payload.GetProperty("data");

            // Ignora mensagens enviadas pelo próprio bot/sistema para evitar loop infinito
            if (data.TryGetProperty("key", out var keyProp) && keyProp.TryGetProperty("fromMe", out var fromMeProp) && fromMeProp.GetBoolean())
            {
                return Ok(new { status = "Ignored own message" });
            }

            // Extrai o identificador do remetente (ex: 5511999999999@s.whatsapp.net)
            string remoteJid = keyProp.GetProperty("remoteJid").GetString() ?? "";

            // Extrai o texto da mensagem enviada pelo paciente
            string messageBody = "";
            if (data.TryGetProperty("message", out var messageProp))
            {
                if (messageProp.TryGetProperty("conversation", out var convProp))
                {
                    messageBody = convProp.GetString() ?? "";
                }
                else if (messageProp.TryGetProperty("extendedTextMessage", out var extProp))
                {
                    if (extProp.TryGetProperty("text", out var textProp))
                    {
                        messageBody = textProp.GetString() ?? "";
                    }
                }
            }

            if (string.IsNullOrEmpty(messageBody) || string.IsNullOrEmpty(remoteJid))
                return BadRequest(new { sucesso = false, erro = "Mensagem ou remetente vazios." });

            _logger.LogInformation("Mensagem recebida de {Remetente} para o Tenant {TenantId}: {Mensagem}", remoteJid, tenantId, messageBody);

            // 2. Processa a mensagem usando o OpenAiService (com Function Calling e regras do Tenant)
            var respostaIa = await _openAiService.GerarRespostaAsync(tenantId, remoteJid, messageBody);

            // 3. Envia a resposta gerada pela IA de volta ao WhatsApp usando o WhatsAppService
            await _whatsAppService.EnviarMensagemAsync(remoteJid, respostaIa);

            return Ok(new { sucesso = true, respostaEnviada = respostaIa });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook da Evolution API.");
            return StatusCode(500, new { sucesso = false, erro = ex.Message });
        }
    }
}