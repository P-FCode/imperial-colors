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

    /// <summary>Apaga (bulk delete, sem carregar linhas em memória) todo log com
    /// <c>DataHora</c> anterior a <paramref name="antesDe"/> — retenção de logs de
    /// auditoria, que sem isso crescem sem limite. Retorna quantas linhas foram removidas.</summary>
    Task<int> ExpurgarAntigosAsync(DateTime antesDe, CancellationToken cancellationToken = default);
}
