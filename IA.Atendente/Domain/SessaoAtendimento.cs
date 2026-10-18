// Domain/SessaoAtendimento.cs
namespace IA.Atendente.Api.Domain;

public class SessaoAtendimento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string IdentificadorCliente { get; set; } = string.Empty; // Ex: Telefone do WhatsApp
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Mensagem> Mensagens { get; set; } = new List<Mensagem>();
}