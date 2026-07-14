using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IMovimentacaoEstoqueRepository : IRepository<MovimentacaoEstoque>
{
    Task<IEnumerable<MovimentacaoEstoque>> ObterPorProdutoAsync(int produtoId);
    Task<IEnumerable<MovimentacaoEstoque>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim);
}
