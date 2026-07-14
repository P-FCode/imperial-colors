using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> ObterDadosDashboardAsync();
}
