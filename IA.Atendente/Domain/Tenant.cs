// Domain/Tenant.cs
namespace IA.Atendente.Api.Domain;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeClinica { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty; // CNPJ
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public AiConfiguration? AiConfiguration { get; set; }
    public ICollection<Servico> Servicos { get; set; } = new List<Servico>();
}