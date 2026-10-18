// Domain/AiConfiguration.cs
namespace IA.Atendente.Api.Domain;

public class AiConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // O cérebro dinâmico por prompt (conforme as diretrizes de multitenancy)
    public string SystemPrompt { get; set; } = string.Empty;
    public string ModeloIa { get; set; } = "gpt-4o-mini";
    public decimal Temperatura { get; set; } = 0.3m;
}