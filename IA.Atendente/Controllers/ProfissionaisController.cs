using IA.Atendente.Api.Data;
using IA.Atendente.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProfissionaisController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfissionaisController(AppDbContext context)
    {
        _context = context;
    }

    // DTO para criação de profissional
    public record CriarProfissionalDto(
        Guid TenantId,
        string Nome,
        string Especialidade
    );

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarProfissionalDto dto)
    {
        var tenantExiste = await _context.Tenants.AnyAsync(t => t.Id == dto.TenantId);
        if (!tenantExiste)
            return NotFound(new { mensagem = "Clínica (Tenant) não encontrada." });

        var profissional = new Profissional
        {
            TenantId = dto.TenantId,
            Nome = dto.Nome,
            Especialidade = dto.Especialidade
        };

        _context.Profissionais.Add(profissional);
        await _context.SaveChangesAsync();

        return Ok(profissional);
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> ListarPorTenant(Guid tenantId)
    {
        var profissionais = await _context.Profissionais
            .Where(p => p.TenantId == tenantId)
            .ToListAsync();

        return Ok(profissionais);
    }
}