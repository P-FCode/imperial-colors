using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface ITributacaoCategoriaRepository
{
    Task<TributacaoCategoria?> ObterPorCategoriaIdAsync(int categoriaId, CancellationToken cancellationToken = default);

    /// <summary>Cria ou atualiza o perfil fiscal padrão da categoria (upsert pela chave categoria_id).</summary>
    Task<TributacaoCategoria> SalvarAsync(TributacaoCategoria tributacao, CancellationToken cancellationToken = default);
}
