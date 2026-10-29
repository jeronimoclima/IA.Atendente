namespace IA.Atendente.Api.Domain;

public class Agendamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid ProfissionalId { get; set; }
    public Profissional Profissional { get; set; } = null!;

    public Guid ServicoId { get; set; }
    public Servico Servico { get; set; } = null!;

    public string IdentificadorCliente { get; set; } = string.Empty; // Ex: Telefone WhatsApp
    public string NomeCliente { get; set; } = string.Empty;

    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }

    public string Status { get; set; } = "Confirmado"; // Confirmado, Cancelado, Concluido
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}