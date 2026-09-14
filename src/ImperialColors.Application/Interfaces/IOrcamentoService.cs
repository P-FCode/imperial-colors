using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.Interfaces;

public interface IOrcamentoService
{
    Task<PaginacaoResultadoDto<OrcamentoDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);

    Task<OrcamentoDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OrcamentoDto> RegistrarAsync(RegistrarOrcamentoDto dto, CancellationToken cancellationToken = default);

    Task<OrcamentoDto> AtualizarAsync(AtualizarOrcamentoDto dto, CancellationToken cancellationToken = default);

    Task<OrcamentoDto> AlterarStatusAsync(int id, StatusOrcamento status, CancellationToken cancellationToken = default);

    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
