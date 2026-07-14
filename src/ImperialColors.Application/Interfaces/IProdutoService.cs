using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IProdutoService
{
    Task<IEnumerable<ProdutoDto>> ObterTodosAsync();
    Task<ProdutoDto?> ObterPorIdAsync(int id);
    Task<ProdutoDto?> ObterPorCodigoBarrasAsync(string codigoBarras);
    Task<ProdutoDto?> ObterPorCodigoInternoAsync(string codigoInterno);
    Task<IEnumerable<ProdutoDto>> BuscarAsync(string termo);
    Task<ProdutoDto> CriarAsync(CriarProdutoDto dto);
    Task<ProdutoDto> AtualizarAsync(int id, AtualizarProdutoDto dto);
    Task RemoverAsync(int id);
    Task<bool> CodigoBarrasExisteAsync(string codigoBarras, int? ignorarProdutoId = null, CancellationToken cancellationToken = default);
    Task<PaginacaoResultadoDto<ProdutoDto>> ObterPaginadoAsync(
        int pagina,
        int itensPorPagina,
        string? termoBusca = null,
        bool apenasPromocao = false,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<ProdutoDto>> ObterComEstoqueBaixoAsync();
    Task<IEnumerable<ProdutoDto>> ObterSemEstoqueAsync();
    Task RegistrarMovimentacaoAsync(MovimentacaoEstoqueDto dto);
    Task<string> GerarProximoCodigoInternoAsync();
    Task<string> GerarCodigoInternoPorNomeAsync(string nome, CancellationToken cancellationToken = default);
}
