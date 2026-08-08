using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Configuração fiscal única da empresa (linha singleton — sempre <see cref="Id"/> = 1),
/// separada dos dados cadastrais do cupom (.env/appsettings) porque serve a um propósito
/// diferente: alimentar o payload de emissão de NF-e/NFC-e (campos <c>emit.enderEmit</c>,
/// <c>ide.tpAmb</c>, <c>ide.serie</c> e o CSC/idCSC do QR Code da NFC-e).
///
/// Preparação de dados — não emite nada sozinha. Confirmado contra o contrato real de
/// uma API de emissão (GUIA_INTEGRACAO.md do PFCode Fiscal): endereço do emitente exige
/// código IBGE do município, não só CEP/cidade em texto livre.
/// </summary>
public class ConfiguracaoFiscalEmpresa
{
    public int Id { get; set; }

    // --- Identificação fiscal adicional (além de CNPJ/Razão Social/IE, que já vêm
    // do .env/appsettings — ver EmpresaConfig) ---
    /// <summary>Checkbox "IE Isento" — quando marcado, a IE não é enviada/validada na NF-e.</summary>
    public bool IeIsenta { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? InscricaoSuframa { get; set; }
    public string? Cnae { get; set; }

    // --- DIFAL (Diferencial de Alíquota do ICMS em operações interestaduais) ---
    /// <summary>EC 87/2015 — partilha do ICMS entre UF de origem e destino quando o
    /// comprador é consumidor final NÃO contribuinte de outro estado.</summary>
    public bool DifalNaoContribuinte { get; set; }

    /// <summary>Cálculo diferenciado de ST quando o destinatário é contribuinte do ICMS
    /// em operação interestadual sujeita a Substituição Tributária.</summary>
    public bool DifalStContribuinte { get; set; }

    // --- Endereço do emitente (enderEmit na NF-e) ---
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }

    /// <summary>Código IBGE do município — 7 dígitos. Obrigatório na NF-e (cMun), sem
    /// fallback possível (não existe "cidade em texto livre" no layout oficial).</summary>
    public string? CodigoMunicipioIbge { get; set; }
    public string? NomeMunicipio { get; set; }
    public string? Uf { get; set; }

    // --- Numeração fiscal ---
    /// <summary>Série da NF-e/NFC-e (0-889). Controle de numeração sequencial próprio,
    /// diferente do número de venda interno do sistema.</summary>
    public string? Serie { get; set; }

    // --- Ambiente de emissão ---
    public AmbienteEmissaoFiscal Ambiente { get; set; } = AmbienteEmissaoFiscal.Homologacao;

    // --- CSC / idCSC (NFC-e) — um par por ambiente, nunca se misturam ---
    public string? IdCscHomologacao { get; set; }
    public string? CscHomologacao { get; set; }
    public string? IdCscProducao { get; set; }
    public string? CscProducao { get; set; }

    /// <summary>Só relevante quando o regime tributário (ConfiguracaoFiscalService) é
    /// Simples Nacional — decide se o CRT enviado é "1" (normal) ou "2" (excesso de
    /// sublimite de receita bruta).</summary>
    public bool SimplesExcessoSublimite { get; set; }

    // --- Reforma Tributária — alíquotas padrão do ano-piloto (2026) ---
    // Fixas e nacionais neste ano (0,1% IBS + 0,9% CBS, todo IBS alocado à UF por
    // padrão) — não variam por produto, por isso vivem aqui e não em TributacaoProduto.
    // A partir de 2027 isso deve ser revisto conforme a transição avança.
    public decimal? AliquotaIbsUfPadrao { get; set; }
    public decimal? AliquotaIbsMunicipioPadrao { get; set; }
    public decimal? AliquotaCbsPadrao { get; set; }

    /// <summary>
    /// "Regra Geral" de CST/cClassTrib do IBS/CBS — usada quando um produto/categoria
    /// não define os próprios (ver TributacaoProduto/TributacaoCategoria, que têm
    /// prioridade sobre este fallback). Equivalente à tela "Configurações gerais para
    /// IBS/CBS" do Olist.
    /// </summary>
    public string? CstIbsCbsPadrao { get; set; }
    public string? CClassTribPadrao { get; set; }

    // --- Configurações gerais de NF-e/NFC-e (comportamento) ---
    public bool ValidarNcmEmNotas { get; set; } = true;
    public bool BloquearEdicaoNumeroNota { get; set; }
    public bool BloquearNotaComItensMenorQueVenda { get; set; } = true;
    public ModalidadeFrete FretePorContaPadrao { get; set; } = ModalidadeFrete.SemOcorrenciaTransporte;
    public string? EmailPadraoEnvioNotas { get; set; }
    public IndicadorPresencaComprador IndicadorPresencaPadrao { get; set; } = IndicadorPresencaComprador.Presencial;

    /// <summary>Preparação para quando a emissão real existir — hoje só guarda a
    /// preferência, não dispara nenhuma chamada de emissão sozinho.</summary>
    public bool GerarNotaAutomaticaAoFinalizarVenda { get; set; }
    public bool CancelarNotaAutomaticoAoCancelarVenda { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public ICollection<InscricaoEstadualSubstituto> InscricoesSubstitutoTributario { get; set; } = new List<InscricaoEstadualSubstituto>();
}
