using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Configuração fiscal global da empresa (não por produto): regime tributário, endereço
/// do emitente para NF-e/NFC-e, série de numeração, ambiente de emissão e CSC/idCSC.
/// </summary>
public interface IConfiguracaoFiscalService
{
    Task<RegimeTributario> ObterRegimeAsync(CancellationToken cancellationToken = default);
    Task DefinirRegimeAsync(RegimeTributario regime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Código CRT (Código de Regime Tributário) exigido no XML da NF-e — combina
    /// <see cref="ObterRegimeAsync"/> com <see cref="ConfiguracaoFiscalEmpresaDto.SimplesExcessoSublimite"/>:
    /// "1" (Simples), "2" (Simples excesso de sublimite), "3" (Regime Normal) ou "4" (MEI).
    /// </summary>
    Task<string> ObterCodigoCrtAsync(CancellationToken cancellationToken = default);

    /// <summary>Configuração fiscal da empresa (endereço, série, ambiente, CSC/idCSC), ou um DTO vazio se nunca foi definida.</summary>
    Task<ConfiguracaoFiscalEmpresaDto> ObterConfiguracaoEmpresaAsync(CancellationToken cancellationToken = default);

    /// <summary>Valida e salva a configuração fiscal da empresa.</summary>
    Task<ConfiguracaoFiscalEmpresaDto> SalvarConfiguracaoEmpresaAsync(ConfiguracaoFiscalEmpresaDto dto, CancellationToken cancellationToken = default);
}
