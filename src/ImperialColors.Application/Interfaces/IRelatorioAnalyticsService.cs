using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IRelatorioAnalyticsService
{
    Task<IReadOnlyList<LinhaRelatorioVendaExternaDto>> ObterLinhasVendasExternasAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoRankingDto>> ObterRankingProdutosAsync(
        DateTime inicio, DateTime fim, TipoAnaliseGiroProduto tipo, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProdutoEncalhadoDto>> ObterProdutosEncalhadosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinhaRelatorioVendaConsolidadaDto>> ObterVendasConsolidadasAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
}
