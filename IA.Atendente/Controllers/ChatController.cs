// Controllers/ChatController.cs
using IA.Atendente.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenAiService _openAiService;

    public ChatController(OpenAiService openAiService)
    {
        _openAiService = openAiService;
    }

    public record EnviarMensagemDto(
        Guid TenantId,
        string IdentificadorCliente, // Ex: Telefone do WhatsApp ou ID do cliente
        string Mensagem
    );

    [HttpPost]
    public async Task<IActionResult> EnviarMensagem([FromBody] EnviarMensagemDto dto)
    {
        try
        {
            var resposta = await _openAiService.GerarRespostaAsync(dto.TenantId, dto.IdentificadorCliente, dto.Mensagem);
            return Ok(new
            {
                sucesso = true,
                respostaDaIa = resposta
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                sucesso = false,
                erro = ex.Message
            });
        }
    }
}