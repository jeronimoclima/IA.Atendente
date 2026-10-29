using IA.Atendente.Api.Data;
using IA.Atendente.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicosController(AppDbContext context)
    {
        _context = context;
    }

    // DTO atualizado para aceitar o tipo de cobrança e quantidade de sessões
    public record CriarServicoDto(
        Guid TenantId,
        string Nome,
        decimal Preco,
        int DuracaoMinutos,
        TipoCobranca TipoCobranca = TipoCobranca.PorMinutos,
        int QuantidadeSessoes = 1
    );

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarServicoDto dto)
    {
        var tenantExiste = await _context.Tenants.AnyAsync(t => t.Id == dto.TenantId);
        if (!tenantExiste)
            return NotFound(new { mensagem = "Clínica (Tenant) não encontrada." });

        var servico = new Servico
        {
            TenantId = dto.TenantId,
            Descricao = dto.Nome,
            Preco = dto.Preco,
            DuracaoMinutos = dto.DuracaoMinutos,
            TipoCobranca = dto.TipoCobranca,
            QuantidadeSessoes = dto.TipoCobranca == TipoCobranca.PorSessao ? dto.QuantidadeSessoes : 1
        };

        _context.Servicos.Add(servico);
        await _context.SaveChangesAsync();

        return Ok(servico);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> ListarPorTenant(Guid tenantId)
    {
        var servicos = await _context.Servicos
            .Where(s => s.TenantId == tenantId)
            .ToListAsync();

        return Ok(servicos);
    }
}