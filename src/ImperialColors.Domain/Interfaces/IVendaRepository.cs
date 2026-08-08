using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IVendaRepository : IRepository<Venda>
{
    Task<Venda?> ObterComItensAsync(int id);
    Task<IEnumerable<Venda>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim);
    Task<decimal> ObterTotalVendasDiaAsync(DateTime data);
    Task<decimal> ObterTotalVendasMesAsync(int ano, int mes);
    Task<string> GerarNumeroVendaAsync();

    /// <summary>
    /// Cria a venda (cabeçalho + itens + pagamentos) e dá baixa atômica no estoque de
    /// cada item numa única transação: se qualquer item ficar sem estoque no momento
    /// exato da baixa, tudo é revertido (nada de venda "meio salva"). O número da venda
    /// também é gerado dentro dessa transação, sob um advisory lock do PostgreSQL, para
    /// que dois PDVs finalizando no mesmo instante nunca gerem o mesmo número.
    /// </summary>
    Task<Venda> CriarComBaixaEstoqueTransacionalAsync(Venda venda, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Venda> Itens, int Total)> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Venda>> ObterUltimasFinalizadasAsync(int quantidade = 5, CancellationToken cancellationToken = default);
    Task CancelarComEstornoAsync(int vendaId, CancellationToken cancellationToken = default);
    Task ExcluirFisicamenteComEstornoAsync(int vendaId, CancellationToken cancellationToken = default);
}
