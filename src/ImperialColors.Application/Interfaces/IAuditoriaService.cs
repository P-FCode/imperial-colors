using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IAuditoriaService
{
    Task RegistrarAsync(RegistrarLogAuditoriaDto dto, CancellationToken cancellationToken = default);

    Task<PaginacaoResultadoDto<LogAuditoriaDto>> ObterPaginadoAsync(
        FiltroLogAuditoriaDto filtro,
        CancellationToken cancellationToken = default);

    Task<LogAuditoriaDto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default);
}
