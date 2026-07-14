using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IFornecedorService
{
    Task<IEnumerable<FornecedorDto>> ObterTodosAsync();
    Task<FornecedorDto?> ObterPorIdAsync(int id);
    Task<IEnumerable<FornecedorDto>> BuscarAsync(string nome);
    Task<FornecedorDto> CriarAsync(FornecedorDto dto);
    Task<FornecedorDto> AtualizarAsync(int id, FornecedorDto dto);
    Task RemoverAsync(int id);
    Task<PaginacaoResultadoDto<FornecedorDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);
}
