using IA.Atendente.Api.Data;
using IA.Atendente.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IA.Atendente.Api.Services;

public class AgendamentoService
{
    private readonly AppDbContext _context;

    public AgendamentoService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Método para verificar se um horário está livre
    public async Task<bool> VerificarDisponibilidadeAsync(Guid tenantId, Guid profissionalId, DateTime inicio, DateTime fim)
    {
        var existeConflito = await _context.Agendamentos
            .AnyAsync(a => a.TenantId == tenantId
                        && a.ProfissionalId == profissionalId
                        && a.Status == "Confirmado"
                        && ((inicio >= a.DataHoraInicio && inicio < a.DataHoraFim) ||
                            (fim > a.DataHoraInicio && fim <= a.DataHoraFim)));

        return !existeConflito;
    }

    // 2. Método para realizar a reserva no banco de dados
    public async Task<(bool Sucesso, string Mensagem, Agendamento? Agendamento)> CriarAgendamentoAsync(
        Guid tenantId,
        string identificadorCliente,
        string nomeCliente,
        Guid profissionalId,
        Guid servicoId,
        DateTime dataHoraInicio)
    {
        var servico = await _context.Servicos.FirstOrDefaultAsync(s => s.Id == servicoId && s.TenantId == tenantId);
        if (servico == null)
            return (false, "Serviço não encontrado.", null);

        var dataHoraFim = dataHoraInicio.AddMinutes(servico.DuracaoMinutos);

        var disponivel = await VerificarDisponibilidadeAsync(tenantId, profissionalId, dataHoraInicio, dataHoraFim);
        if (!disponivel)
            return (false, "Desculpe, o profissional já possui agendamento neste horário.", null);

        var agendamento = new Agendamento
        {
            TenantId = tenantId,
            IdentificadorCliente = identificadorCliente,
            NomeCliente = nomeCliente,
            ProfissionalId = profissionalId,
            ServicoId = servicoId,
            DataHoraInicio = dataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = "Confirmado"
        };

        _context.Agendamentos.Add(agendamento);
        await _context.SaveChangesAsync();

        return (true, "Agendamento realizado com sucesso!", agendamento);
    }
}