// Controllers/ClinicaController.cs
using IA.Atendente.Api.Data;
using IA.Atendente.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicaController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClinicaController(AppDbContext context)
    {
        _context = context;
    }

    // DTO para cadastro completo da clínica com prompt e serviços iniciais
    public record CriarClinicaDto(
        string NomeClinica,
        string Documento,
        string SystemPrompt,
        string ModeloIa,
        decimal Temperatura
    );

    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CriarClinicaDto dto)
    {
        var tenant = new Tenant
        {
            NomeClinica = dto.NomeClinica,
            Documento = dto.Documento,
            AiConfiguration = new AiConfiguration
            {
                SystemPrompt = dto.SystemPrompt,
                ModeloIa = string.IsNullOrEmpty(dto.ModeloIa) ? "gpt-4o-mini" : dto.ModeloIa,
                Temperatura = dto.Temperatura == 0 ? 0.3m : dto.Temperatura
            }
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = tenant.Id }, tenant);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.AiConfiguration)
            .Include(t => t.Servicos)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
            return NotFound(new { mensagem = "Clínica não encontrada." });

        return Ok(tenant);
    }
}