using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

/// <summary>Repositório da linha singleton de configuração fiscal da empresa.</summary>
public interface IConfiguracaoFiscalEmpresaRepository
{
    Task<ConfiguracaoFiscalEmpresa?> ObterAsync(CancellationToken cancellationToken = default);

    /// <summary>Cria (na primeira vez) ou atualiza a única linha de configuração.</summary>
    Task<ConfiguracaoFiscalEmpresa> SalvarAsync(ConfiguracaoFiscalEmpresa configuracao, CancellationToken cancellationToken = default);
}
