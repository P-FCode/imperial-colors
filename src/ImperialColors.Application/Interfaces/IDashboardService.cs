using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> ObterDadosDashboardAsync();

    /// <summary>Visão "Estoque" do Dashboard — produtos próximos da validade, com pouca
    /// quantidade, e mais vendidos do mês. Carregada sob demanda (só quando o operador troca
    /// para essa visão), não junto com o financeiro.</summary>
    Task<DashboardEstoqueDto> ObterVisaoEstoqueAsync(CancellationToken cancellationToken = default);

    /// <summary>Visão "Vendas" do Dashboard — maiores vendas do mês.</summary>
    Task<DashboardVendasDto> ObterVisaoVendasAsync(CancellationToken cancellationToken = default);
}
