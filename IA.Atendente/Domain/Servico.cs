// Domain/Servico.cs
namespace IA.Atendente.Api.Domain;

public class Servico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty; // Ex: Limpeza de Pele, Criolipólise
    public decimal Preco { get; set; }
    public int Duracao { get; set; }
}