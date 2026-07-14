using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IVendaRepository : IRepository<Venda>
{
    Task<Venda?> ObterComItensAsync(int id);
    Task<IEnumerable<Venda>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim);
    Task<decimal> ObterTotalVendasDiaAsync(DateTime data);
    Task<decimal> ObterTotalVendasMesAsync(int ano, int mes);
    Task<string> GerarNumeroVendaAsync();
    Task<(IReadOnlyList<Venda> Itens, int Total)> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venda>> ObterUltimasFinalizadasAsync(int quantidade = 5, CancellationToken cancellationToken = default);
    Task CancelarComEstornoAsync(int vendaId, CancellationToken cancellationToken = default);
    Task ExcluirFisicamenteComEstornoAsync(int vendaId, CancellationToken cancellationToken = default);
}
