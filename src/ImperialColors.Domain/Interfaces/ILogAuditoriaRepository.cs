using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Interfaces;

public interface ILogAuditoriaRepository
{
    Task AdicionarAsync(LogAuditoria log, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LogAuditoria> Itens, int Total)> ObterPaginadoAsync(
        DateTime? dataInicio,
        DateTime? dataFim,
        NivelLogAuditoria? nivel,
        string? modulo,
        string? termoBusca,
        int pagina,
        int itensPorPagina,
        CancellationToken cancellationToken = default);

    Task<LogAuditoria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
}
