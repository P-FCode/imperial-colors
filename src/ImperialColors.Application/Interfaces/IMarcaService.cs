using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IMarcaService
{
    Task<IEnumerable<MarcaDto>> ObterTodosAsync();
    Task<MarcaDto> CriarAsync(string nome);
}
