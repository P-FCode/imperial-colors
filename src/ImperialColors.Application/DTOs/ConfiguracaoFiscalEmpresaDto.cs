using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

/// <summary>
/// Configuração fiscal da empresa: endereço estruturado do emitente (para NF-e/NFC-e),
/// série de numeração fiscal, ambiente de emissão, CSC/idCSC por ambiente, DIFAL,
/// Regra Geral do IBS/CBS e comportamentos gerais de emissão.
/// </summary>
public class ConfiguracaoFiscalEmpresaDto
{
    public bool IeIsenta { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? InscricaoSuframa { get; set; }
    public string? Cnae { get; set; }

    public bool DifalNaoContribuinte { get; set; }
    public bool DifalStContribuinte { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? CodigoMunicipioIbge { get; set; }
    public string? NomeMunicipio { get; set; }
    public string? Uf { get; set; }

    public string? Serie { get; set; }

    public AmbienteEmissaoFiscal Ambiente { get; set; } = AmbienteEmissaoFiscal.Homologacao;

    public string? IdCscHomologacao { get; set; }
    public string? CscHomologacao { get; set; }
    public string? IdCscProducao { get; set; }
    public string? CscProducao { get; set; }

    public bool SimplesExcessoSublimite { get; set; }

    public decimal? AliquotaIbsUfPadrao { get; set; }
    public decimal? AliquotaIbsMunicipioPadrao { get; set; }
    public decimal? AliquotaCbsPadrao { get; set; }
    public string? CstIbsCbsPadrao { get; set; }
    public string? CClassTribPadrao { get; set; }

    public bool ValidarNcmEmNotas { get; set; } = true;
    public bool BloquearEdicaoNumeroNota { get; set; }
    public bool BloquearNotaComItensMenorQueVenda { get; set; } = true;
    public ModalidadeFrete FretePorContaPadrao { get; set; } = ModalidadeFrete.SemOcorrenciaTransporte;
    public string? EmailPadraoEnvioNotas { get; set; }
    public IndicadorPresencaComprador IndicadorPresencaPadrao { get; set; } = IndicadorPresencaComprador.Presencial;
    public bool GerarNotaAutomaticaAoFinalizarVenda { get; set; }
    public bool CancelarNotaAutomaticoAoCancelarVenda { get; set; }

    public List<InscricaoEstadualSubstitutoDto> InscricoesSubstitutoTributario { get; set; } = new();

    /// <summary>True quando o endereço fiscal já foi preenchido (ao menos CEP e IBGE).</summary>
    public bool EnderecoPreenchido =>
        !string.IsNullOrWhiteSpace(Cep) && !string.IsNullOrWhiteSpace(CodigoMunicipioIbge);
}

public class InscricaoEstadualSubstitutoDto
{
    public string Uf { get; set; } = string.Empty;
    public string InscricaoEstadual { get; set; } = string.Empty;
}
