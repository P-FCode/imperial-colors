using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaDto>> ObterTodosAsync();
    Task<CategoriaDto> CriarAsync(string nome);

    /// <summary>Perfil fiscal padrão da categoria, ou um DTO vazio se nunca foi definido.</summary>
    Task<TributacaoCategoriaDto> ObterTributacaoPadraoAsync(int categoriaId, CancellationToken cancellationToken = default);

    /// <summary>Valida e salva o perfil fiscal padrão da categoria.</summary>
    Task<TributacaoCategoriaDto> SalvarTributacaoPadraoAsync(int categoriaId, TributacaoCategoriaDto dto, CancellationToken cancellationToken = default);
}
