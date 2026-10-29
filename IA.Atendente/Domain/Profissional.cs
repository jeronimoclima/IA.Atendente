namespace IA.Atendente.Api.Domain;

public class Profissional
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Nome { get; set; } = string.Empty; // Ex: Dra. Camila Silva
    public string Especialidade { get; set; } = string.Empty; // Ex: Dermatologia

    public ICollection<Agendamento> Agendamentos { get; set; } = new List<Agendamento>();
}