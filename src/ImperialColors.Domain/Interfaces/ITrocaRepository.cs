using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface ITrocaRepository : IRepository<Troca>
{
    Task<IEnumerable<Troca>> ObterPorVendaAsync(int vendaId);
    Task RegistrarTrocaTransacionalAsync(
        Troca troca,
        Produto produtoDevolvido,
        Produto produtoNovo,
        bool retornarAoEstoque,
        CancellationToken cancellationToken = default);
    Task RegistrarTrocaVendaExternaTransacionalAsync(
        Troca troca,
        Produto produtoDevolvido,
        Produto produtoNovo,
        bool retornarAoEstoque,
        CancellationToken cancellationToken = default);
}
