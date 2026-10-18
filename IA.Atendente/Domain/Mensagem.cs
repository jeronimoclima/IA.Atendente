// Domain/Mensagem.cs
namespace IA.Atendente.Api.Domain;

public class Mensagem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessaoAtendimentoId { get; set; }
    public SessaoAtendimento SessaoAtendimento { get; set; } = null!;

    public string Role { get; set; } = string.Empty; // "user" ou "assistant"
    public string Conteudo { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}