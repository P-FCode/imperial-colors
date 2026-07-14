using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface ITrocaService
{
    Task<TrocaDto> RegistrarAsync(RegistrarTrocaDto dto, CancellationToken cancellationToken = default);
    Task<TrocaDto> RegistrarVendaExternaAsync(RegistrarTrocaVendaExternaDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<TrocaDto>> ObterPorVendaAsync(int vendaId);
}
