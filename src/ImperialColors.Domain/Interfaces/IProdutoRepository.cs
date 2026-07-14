using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IProdutoRepository : IRepository<Produto>
{
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
