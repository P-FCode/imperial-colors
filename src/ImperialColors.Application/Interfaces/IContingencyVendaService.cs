using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IContingencyVendaService
{
    Task<VendaDto> SalvarVendaOfflineAsync(CriarVendaDto dto, CancellationToken cancellationToken = default);
    Task AtualizarCacheProdutosAsync(CancellationToken cancellationToken = default);
    Task<decimal?> ObterEstoqueLocalAsync(int produtoId, CancellationToken cancellationToken = default);
    Task<int> ContarPendentesAsync(CancellationToken cancellationToken = default);
}
