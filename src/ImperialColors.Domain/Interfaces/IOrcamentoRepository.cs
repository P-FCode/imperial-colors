using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Interfaces;

public interface IOrcamentoRepository : IRepository<Orcamento>
{
    /// <summary>Paginação no banco (Skip/Take) — a tabela cresce com o movimento da loja.</summary>
    Task<(IReadOnlyList<Orcamento> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);

    Task<Orcamento?> ObterComItensAsync(int id, CancellationToken cancellationToken = default);

    Task<Orcamento> RegistrarTransacionalAsync(
        Orcamento orcamento,
        IReadOnlyList<ItemOrcamento> itens,
        CancellationToken cancellationToken = default);

    Task<Orcamento> AtualizarTransacionalAsync(
        Orcamento orcamento,
        IReadOnlyList<ItemOrcamento> itens,
        CancellationToken cancellationToken = default);

    Task<Orcamento> AlterarStatusAsync(
        int id, StatusOrcamento status, CancellationToken cancellationToken = default);
}
