using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Permite ao sincronizador de contingência gravar a venda no PostgreSQL
/// com chave de idempotência, sem reentrar no caminho offline.
/// </summary>
public interface IVendaContingenciaSync
{
    Task<VendaDto> CriarComContingenciaIdAsync(
        CriarVendaDto dto,
        Guid contingenciaId,
        CancellationToken cancellationToken = default);
}
