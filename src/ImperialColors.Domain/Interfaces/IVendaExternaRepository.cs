using ImperialColors.Domain.Entities;
using ImperialColors.Domain.ReadModels;

namespace ImperialColors.Domain.Interfaces;

public interface IVendaExternaRepository : IRepository<VendaExterna>
{
    Task<IEnumerable<VendaExterna>> ObterTodosComItensAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<VendaExterna>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    /// <summary>Faturamento, custo e quantidade por dia, agregados pelo banco — o par do
    /// resumo de <see cref="IVendaRepository.ObterResumoDiarioAsync"/>, para o Dashboard
    /// somar balcão e rua sem materializar as duas listas de vendas na memória.</summary>
    Task<IReadOnlyList<ResumoVendasDiario>> ObterResumoDiarioAsync(
        DateTime inicio, DateTime fimExclusivo, CancellationToken cancellationToken = default);

    /// <summary>Paginação real no banco (Skip/Take) — diferente de <see cref="ObterTodosComItensAsync"/>,
    /// que carrega a tabela inteira com Include(Itens) sempre que a tela abre.</summary>
    Task<(IReadOnlyList<VendaExterna> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);
    Task<VendaExterna?> ObterComItensAsync(int id, CancellationToken cancellationToken = default);
    Task<string> GerarNumeroVendaExternaAsync(CancellationToken cancellationToken = default);
    Task<VendaExterna> RegistrarTransacionalAsync(
        VendaExterna venda,
        IReadOnlyList<ItemVendaExterna> itens,
        string? usuario,
        CancellationToken cancellationToken = default);
    Task<VendaExterna> AtualizarTransacionalAsync(
        int vendaId,
        string? observacoes,
        IReadOnlyList<ItemVendaExterna> itens,
        string? usuario,
        CancellationToken cancellationToken = default);
    Task ExcluirFisicamenteTransacionalAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> PossuiTrocasAsync(int vendaExternaId, CancellationToken cancellationToken = default);
}
