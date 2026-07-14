using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaDto>> ObterTodosAsync();
    Task<CategoriaDto> CriarAsync(string nome);
}
