using ImperialColors.Domain.ReadModels;

namespace ImperialColors.Domain.Interfaces;

public interface IRelatorioAnalyticsRepository
{
    Task<IReadOnlyList<LinhaRelatorioVendaExternaResumo>> ObterLinhasVendasExternasPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoRankingResumo>> ObterProdutosMaisVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoRankingResumo>> ObterProdutosMenosVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoEncalhadoResumo>> ObterProdutosNuncaVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
}
