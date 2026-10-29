namespace IA.Atendente.Api.Domain;

public enum TipoCobranca
{
    PorMinutos = 1, // Ex: Consulta de 30 min, Limpeza de Pele 60 min
    PorSessao = 2   // Ex: Pacote de 10 sessões de Laser, Aplicação pontual
}

public class Servico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Descricao { get; set; } = string.Empty; // Ex: Limpeza de Pele, Pacote Laser 10 Sessões
    public decimal Preco { get; set; }

    // Duração estimada de cada atendimento individual em minutos (Ex: 40 min por sessão)
    public int DuracaoMinutos { get; set; } = 30;

    // Indica se é cobrado por minutos de atendimento ou por pacote/sessão
    public TipoCobranca TipoCobranca { get; set; } = TipoCobranca.PorMinutos;

    // Quantidade de sessões inclusas no valor (Se TipoCobranca == PorSessao)
    public int QuantidadeSessoes { get; set; } = 1;
}