using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class ConfiguracaoFiscalService : IConfiguracaoFiscalService
{
    // Simples Nacional é o regime mais comum para o porte de negócio deste sistema
    // (loja/varejo) — usado como padrão até o administrador configurar explicitamente.
    private const RegimeTributario RegimePadrao = RegimeTributario.SimplesNacional;

    private readonly IParametroSistemaRepository _parametroRepository;
    private readonly IConfiguracaoFiscalEmpresaRepository _empresaRepository;

    public ConfiguracaoFiscalService(
        IParametroSistemaRepository parametroRepository,
        IConfiguracaoFiscalEmpresaRepository empresaRepository)
    {
        _parametroRepository = parametroRepository;
        _empresaRepository = empresaRepository;
    }

    public async Task<RegimeTributario> ObterRegimeAsync(CancellationToken cancellationToken = default)
    {
        var texto = await _parametroRepository.ObterTextoAsync(
            ParametroSistemaChaves.RegimeTributarioEmpresa, cancellationToken);

        return Enum.TryParse<RegimeTributario>(texto, out var regime) ? regime : RegimePadrao;
    }

    public Task DefinirRegimeAsync(RegimeTributario regime, CancellationToken cancellationToken = default)
        => _parametroRepository.SalvarTextoAsync(
            ParametroSistemaChaves.RegimeTributarioEmpresa, regime.ToString(), cancellationToken);

    public async Task<string> ObterCodigoCrtAsync(CancellationToken cancellationToken = default)
    {
        var regime = await ObterRegimeAsync(cancellationToken);

        if (regime == RegimeTributario.Mei)
            return "4";

        if (regime == RegimeTributario.SimplesNacional)
        {
            var empresa = await _empresaRepository.ObterAsync(cancellationToken);
            return empresa is { SimplesExcessoSublimite: true } ? "2" : "1";
        }

        // LucroPresumido e LucroReal mapeiam ambos para CRT 3 — o CRT não distingue os dois.
        return "3";
    }

    public async Task<ConfiguracaoFiscalEmpresaDto> ObterConfiguracaoEmpresaAsync(CancellationToken cancellationToken = default)
    {
        var configuracao = await _empresaRepository.ObterAsync(cancellationToken);
        return configuracao is null ? new ConfiguracaoFiscalEmpresaDto() : MapParaDto(configuracao);
    }

    public async Task<ConfiguracaoFiscalEmpresaDto> SalvarConfiguracaoEmpresaAsync(
        ConfiguracaoFiscalEmpresaDto dto, CancellationToken cancellationToken = default)
    {
        ConfiguracaoFiscalEmpresaValidator.Validar(dto);

        var entidade = new ConfiguracaoFiscalEmpresa
        {
            IeIsenta = dto.IeIsenta,
            InscricaoMunicipal = TextoOuNulo(dto.InscricaoMunicipal),
            InscricaoSuframa = TextoOuNulo(dto.InscricaoSuframa),
            Cnae = NormalizarDigitos(dto.Cnae),
            DifalNaoContribuinte = dto.DifalNaoContribuinte,
            DifalStContribuinte = dto.DifalStContribuinte,
            Cep = NormalizarDigitos(dto.Cep),
            Logradouro = TextoOuNulo(dto.Logradouro),
            Numero = TextoOuNulo(dto.Numero),
            Complemento = TextoOuNulo(dto.Complemento),
            Bairro = TextoOuNulo(dto.Bairro),
            CodigoMunicipioIbge = NormalizarDigitos(dto.CodigoMunicipioIbge),
            NomeMunicipio = TextoOuNulo(dto.NomeMunicipio),
            Uf = string.IsNullOrWhiteSpace(dto.Uf) ? null : dto.Uf.Trim().ToUpperInvariant(),
            Serie = NormalizarDigitos(dto.Serie),
            Ambiente = dto.Ambiente,
            IdCscHomologacao = TextoOuNulo(dto.IdCscHomologacao),
            CscHomologacao = TextoOuNulo(dto.CscHomologacao),
            IdCscProducao = TextoOuNulo(dto.IdCscProducao),
            CscProducao = TextoOuNulo(dto.CscProducao),
            SimplesExcessoSublimite = dto.SimplesExcessoSublimite,
            AliquotaIbsUfPadrao = dto.AliquotaIbsUfPadrao,
            AliquotaIbsMunicipioPadrao = dto.AliquotaIbsMunicipioPadrao,
            AliquotaCbsPadrao = dto.AliquotaCbsPadrao,
            CstIbsCbsPadrao = NormalizarDigitos(dto.CstIbsCbsPadrao),
            CClassTribPadrao = NormalizarDigitos(dto.CClassTribPadrao),
            ValidarNcmEmNotas = dto.ValidarNcmEmNotas,
            BloquearEdicaoNumeroNota = dto.BloquearEdicaoNumeroNota,
            BloquearNotaComItensMenorQueVenda = dto.BloquearNotaComItensMenorQueVenda,
            FretePorContaPadrao = dto.FretePorContaPadrao,
            EmailPadraoEnvioNotas = TextoOuNulo(dto.EmailPadraoEnvioNotas),
            IndicadorPresencaPadrao = dto.IndicadorPresencaPadrao,
            GerarNotaAutomaticaAoFinalizarVenda = dto.GerarNotaAutomaticaAoFinalizarVenda,
            CancelarNotaAutomaticoAoCancelarVenda = dto.CancelarNotaAutomaticoAoCancelarVenda,
            InscricoesSubstitutoTributario = dto.InscricoesSubstitutoTributario
                .Select(i => new InscricaoEstadualSubstituto
                {
                    Uf = i.Uf.Trim().ToUpperInvariant(),
                    InscricaoEstadual = i.InscricaoEstadual.Trim()
                })
                .ToList()
        };

        var salvo = await _empresaRepository.SalvarAsync(entidade, cancellationToken);
        return MapParaDto(salvo);
    }

    private static string? NormalizarDigitos(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string? TextoOuNulo(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ConfiguracaoFiscalEmpresaDto MapParaDto(ConfiguracaoFiscalEmpresa c) => new()
    {
        IeIsenta = c.IeIsenta,
        InscricaoMunicipal = c.InscricaoMunicipal,
        InscricaoSuframa = c.InscricaoSuframa,
        Cnae = c.Cnae,
        DifalNaoContribuinte = c.DifalNaoContribuinte,
        DifalStContribuinte = c.DifalStContribuinte,
        Cep = c.Cep,
        Logradouro = c.Logradouro,
        Numero = c.Numero,
        Complemento = c.Complemento,
        Bairro = c.Bairro,
        CodigoMunicipioIbge = c.CodigoMunicipioIbge,
        NomeMunicipio = c.NomeMunicipio,
        Uf = c.Uf,
        Serie = c.Serie,
        Ambiente = c.Ambiente,
        IdCscHomologacao = c.IdCscHomologacao,
        CscHomologacao = c.CscHomologacao,
        IdCscProducao = c.IdCscProducao,
        CscProducao = c.CscProducao,
        SimplesExcessoSublimite = c.SimplesExcessoSublimite,
        AliquotaIbsUfPadrao = c.AliquotaIbsUfPadrao,
        AliquotaIbsMunicipioPadrao = c.AliquotaIbsMunicipioPadrao,
        AliquotaCbsPadrao = c.AliquotaCbsPadrao,
        CstIbsCbsPadrao = c.CstIbsCbsPadrao,
        CClassTribPadrao = c.CClassTribPadrao,
        ValidarNcmEmNotas = c.ValidarNcmEmNotas,
        BloquearEdicaoNumeroNota = c.BloquearEdicaoNumeroNota,
        BloquearNotaComItensMenorQueVenda = c.BloquearNotaComItensMenorQueVenda,
        FretePorContaPadrao = c.FretePorContaPadrao,
        EmailPadraoEnvioNotas = c.EmailPadraoEnvioNotas,
        IndicadorPresencaPadrao = c.IndicadorPresencaPadrao,
        GerarNotaAutomaticaAoFinalizarVenda = c.GerarNotaAutomaticaAoFinalizarVenda,
        CancelarNotaAutomaticoAoCancelarVenda = c.CancelarNotaAutomaticoAoCancelarVenda,
        InscricoesSubstitutoTributario = c.InscricoesSubstitutoTributario
            .Select(i => new InscricaoEstadualSubstitutoDto { Uf = i.Uf, InscricaoEstadual = i.InscricaoEstadual })
            .OrderBy(i => i.Uf)
            .ToList()
    };
}
