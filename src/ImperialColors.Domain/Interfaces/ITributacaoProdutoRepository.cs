using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface ITributacaoProdutoRepository
{
    Task<TributacaoProduto?> ObterPorProdutoIdAsync(int produtoId, CancellationToken cancellationToken = default);

    /// <summary>Cria ou atualiza a tributação do produto (upsert pela chave produto_id).</summary>
    Task<TributacaoProduto> SalvarAsync(TributacaoProduto tributacao, CancellationToken cancellationToken = default);
}
