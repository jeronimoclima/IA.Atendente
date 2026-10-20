namespace IA.Atendente.Api.Domain;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeClinica { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty; // CNPJ
    public string Endereco { get; set; } = string.Empty; // Localização da clínica
    public string HorarioFuncionamento { get; set; } = string.Empty; // Horários de atendimento
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public AiConfiguration? AiConfiguration { get; set; }
    public ICollection<Servico> Servicos { get; set; } = new List<Servico>();
}