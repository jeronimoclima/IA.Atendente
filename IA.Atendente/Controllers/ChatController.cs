// Controllers/ChatController.cs
using IA.Atendente.Api.Data;
using IA.Atendente.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenAiService _openAiService;
    private readonly AppDbContext _context;

    public ChatController(OpenAiService openAiService, AppDbContext context)
    {
        _openAiService = openAiService;
        _context = context;
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
    [HttpGet("historico/{tenantId:guid}/{identificadorCliente}")]
    public async Task<IActionResult> ObterHistorico(Guid tenantId, string identificadorCliente)
    {
        var sessao = await _context.SessoesAtendimento
            .Include(s => s.Mensagens)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.IdentificadorCliente == identificadorCliente);

        if (sessao == null)
            return NotFound(new { mensagem = "Nenhuma sessão encontrada para este cliente." });

        var historicoDto = sessao.Mensagens
            .OrderBy(m => m.CriadoEm)
            .Select(m => new
            {
                m.Role,
                m.Conteudo,
                m.CriadoEm
            });

        return Ok(historicoDto);
    }
}