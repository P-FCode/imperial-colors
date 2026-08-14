using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IVendaService
{
    // Sem ObterTodosAsync: carregar todas as vendas do histórico não tem uso legítimo em
    // nenhuma tela — quem lista usa ObterPaginadoPorPeriodoAsync, quem soma usa os totais.
    Task<VendaDto?> ObterPorIdAsync(int id);
    Task<VendaDto?> ObterComItensAsync(int id);
    Task<IEnumerable<VendaDto>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim);
    Task<PaginacaoResultadoDto<VendaDto>> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default);
    Task<VendaDto> CriarAsync(CriarVendaDto dto);
    Task<VendaDto> FinalizarAsync(int id);
    Task CancelarAsync(int id);
    Task ExcluirFisicamenteAsync(int id);
    Task<decimal> ObterTotalVendasDiaAsync();
    Task<decimal> ObterTotalVendasMesAsync();
}
