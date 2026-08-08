using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Calcula os totais fiscais (equivalentes a ICMSTot/IBSCBSTot da NF-e) de uma venda já
/// registrada, a partir da tributação cadastrada de cada produto. Preparação para a
/// futura montagem do payload de emissão — não emite nada, só calcula e sinaliza avisos
/// quando encontra um item sem dados suficientes.
/// </summary>
public interface ICalculoFiscalVendaService
{
    Task<TotaisFiscaisVendaDto> CalcularAsync(int vendaId, CancellationToken cancellationToken = default);
}
