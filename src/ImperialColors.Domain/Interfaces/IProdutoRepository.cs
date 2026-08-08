using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Interfaces;

public interface IProdutoRepository : IRepository<Produto>
{
    /// <summary>
    /// Aplica uma movimentação de estoque (entrada/saída/ajuste) e grava o respectivo
    /// registro em <see cref="MovimentacaoEstoque"/> numa única transação, usando UPDATE
    /// atômico guardado para saída (nunca deixa o estoque negativo por concorrência).
    /// </summary>
    Task<MovimentacaoEstoque> AjustarEstoqueTransacionalAsync(
        int produtoId,
        TipoMovimentacao tipo,
        decimal quantidade,
        string? motivo,
        string? usuario,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza os campos comerciais do produto e, na mesma transação, ajusta o estoque
    /// pelo <b>delta pretendido pelo usuário</b>: a diferença entre o que ele digitou
    /// (<paramref name="quantidadeDesejada"/>) e o valor que ele tinha na tela quando abriu
    /// a edição (<paramref name="quantidadeBaseline"/>) — não a diferença contra o valor
    /// atual do banco. Esse delta é então aplicado de forma atômica sobre o valor real e
    /// atual do banco (via UPDATE guardado).
    ///
    /// Isso é o que garante que editar um produto sem tocar no campo de quantidade
    /// (delta = 0) nunca apague uma baixa/reposição feita por uma venda concorrente no PDV
    /// enquanto o formulário estava aberto — e que, quando o usuário realmente ajusta a
    /// quantidade, o ajuste é somado/subtraído do estoque real, não sobrescreve um valor
    /// absoluto obsoleto.
    /// </summary>
    Task<Produto> AtualizarComAjusteEstoqueTransacionalAsync(
        Produto produto,
        decimal quantidadeBaseline,
        decimal quantidadeDesejada,
        string? motivoAjuste,
        string? usuarioAjuste,
        CancellationToken cancellationToken = default);

    Task<Produto?> ObterPorCodigoInternoAsync(string codigoInterno);
    Task<Produto?> ObterPorCodigoBarrasAsync(string codigoBarras);
    Task<IEnumerable<Produto>> BuscarPorNomeAsync(string nome);
    Task<IEnumerable<Produto>> ObterComEstoqueBaixoAsync();
    Task<IEnumerable<Produto>> ObterSemEstoqueAsync();
    Task<int> ContarComEstoqueCriticoAsync(decimal limiteUnidades = 5);
    Task<IEnumerable<Produto>> ObterComCategoriaEMarcaAsync();
    Task<bool> CodigoInternoExisteAsync(string codigoInterno, int? ignorarId = null);
    Task<int> ObterMaiorSequenciaCodigoInternoAsync();
    Task<int> ObterMaiorSequenciaPorSiglaAsync(string sigla, CancellationToken cancellationToken = default);
    Task<Produto> InserirProdutoAsync(Produto produto, bool permitirRegenerarCodigoInterno, Func<Task<string>> obterProximoCodigoInternoAsync);
    Task<bool> CodigoBarrasExisteAsync(string codigoBarras, int? ignorarId = null, CancellationToken cancellationToken = default);
    Task<bool> PossuiHistoricoComercialAsync(int produtoId, CancellationToken cancellationToken = default);
    Task RemoverFisicamenteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteFisicamenteAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Produto> Itens, int Total)> ObterPaginadoAsync(
        int pagina,
        int itensPorPagina,
        string? termoBusca = null,
        bool apenasPromocao = false,
        CancellationToken cancellationToken = default);
}
