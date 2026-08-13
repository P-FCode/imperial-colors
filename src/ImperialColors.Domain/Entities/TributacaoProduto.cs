using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Dados fiscais de um produto (NCM, CEST, ICMS, PIS/COFINS, IPI, unidade tributável).
/// Relação 1:1 opcional com <see cref="Produto"/> (chave primária compartilhada:
/// <see cref="ProdutoId"/>) — só existe uma linha aqui quando o produto já teve sua
/// tributação preenchida. Preparado para alimentar uma futura emissão de NF-e/NFC-e,
/// onde NCM, CST/CSOSN e origem incorretos causam rejeição pela SEFAZ.
/// </summary>
public class TributacaoProduto
{
    /// <summary>Chave primária = chave estrangeira para Produto (relação 1:1).</summary>
    public int ProdutoId { get; set; }

    /// <summary>Nomenclatura Comum do Mercosul — 8 dígitos numéricos.</summary>
    public string? Ncm { get; set; }

    /// <summary>Código Especificador da Substituição Tributária — 7 dígitos numéricos.</summary>
    public string? Cest { get; set; }

    public OrigemMercadoria? Origem { get; set; }

    // --- ICMS ---
    // CST (regime normal: Lucro Presumido/Real) e CSOSN (Simples Nacional) são
    // mutuamente exclusivos — apenas um dos dois deve estar preenchido, conforme o
    // regime tributário configurado da empresa (ver RegimeTributario).
    public string? CstIcms { get; set; }
    public string? CsosnIcms { get; set; }
    public decimal? AliquotaIcms { get; set; }
    public decimal? AliquotaIcmsSt { get; set; }
    public decimal? Mva { get; set; }
    public decimal? ReducaoBaseCalculo { get; set; }

    /// <summary>Percentual do ICMS-ST retido (<c>pST</c>) — só para produtos com CST 60 /
    /// CSOSN 500 (ICMS-ST já cobrado por um elo anterior da cadeia, ex.: fabricante/
    /// distribuidor). Informado pelo cadastro (normalmente vindo da nota de compra ou de
    /// tabela do fisco estadual), não calculado por fórmula — ao contrário do MVA, que serve
    /// para calcular ST "para frente" (CST 10/CSOSN 201/202/203), este percentual já é o
    /// valor final a declarar.</summary>
    public decimal? AliquotaIcmsStRetido { get; set; }

    // --- PIS ---
    public string? CstPis { get; set; }
    public decimal? AliquotaPis { get; set; }

    // --- COFINS ---
    public string? CstCofins { get; set; }
    public decimal? AliquotaCofins { get; set; }

    // --- IPI ---
    public string? CstIpi { get; set; }
    public string? CodigoEnquadramentoIpi { get; set; }
    public decimal? AliquotaIpi { get; set; }

    /// <summary>Valor fixo do IPI (R$) — só para produtos com tributação específica por
    /// valor fixo em vez de percentual (raro; a maioria usa <see cref="AliquotaIpi"/>).</summary>
    public decimal? ValorIpiFixo { get; set; }

    /// <summary>Código EX da TIPI — exceção da Tabela de Incidência do IPI para este NCM.</summary>
    public string? ExTipi { get; set; }

    // --- Unidade tributável ---
    // A unidade comercial (Produto.Unidade) pode divergir da unidade usada para
    // tributação na nota fiscal (ex.: vende em "GL" mas tributa em "LT").
    public string? UnidadeTributavel { get; set; }
    public decimal? FatorConversao { get; set; }
    public string? GtinTributavel { get; set; }

    // --- CFOP (Código Fiscal de Operações e Prestações) ---
    // Obrigatório por item em toda NF-e/NFC-e — 4 dígitos. Guardamos dois "padrões"
    // porque o CFOP muda conforme o destino da venda (mesmo estado × outro estado),
    // não é um valor fixo único por produto: 1xxx/5xxx = operação interna (dentro do
    // estado do emitente), 2xxx/6xxx = interestadual. A venda real decide qual dos
    // dois usar comparando a UF do emitente com a do destinatário (ou "dentro do
    // estado" por padrão em venda de balcão sem CPF/CNPJ do comprador).
    /// <summary>CFOP para venda dentro do estado do emitente — normalmente começa com 5 (ex.: 5102).</summary>
    public string? CfopDentroEstado { get; set; }

    /// <summary>CFOP para venda para outro estado — normalmente começa com 6 (ex.: 6102).</summary>
    public string? CfopForaEstado { get; set; }

    // --- Reforma Tributária (IBS/CBS) — LC 214/2025, NT 2025.002 ---
    // Estrutura nova e paralela ao ICMS/PIS/COFINS acima, não uma extensão deles.
    // As ALÍQUOTAS do IBS/CBS não ficam aqui: no ano-piloto (2026) são fixas e
    // nacionais (não variam por produto), então vivem em
    // ConfiguracaoFiscalEmpresa.AliquotaIbsUfPadrao/AliquotaIbsMunicipioPadrao/
    // AliquotaCbsPadrao (uma única vez para a empresa toda). Isso muda a partir de
    // 2027+ conforme a transição avança — reavaliar quando for integrar de verdade.
    //
    /// <summary>CST do IBS/CBS — 3 dígitos (ex.: 000, 200, 400, 620). Único, vale para
    /// Simples Nacional e Regime Normal (diferente do CST×CSOSN do ICMS).</summary>
    public string? CstIbsCbs { get; set; }

    /// <summary>Código de Classificação Tributária — 6 dígitos, formato CSTYYY (os 3
    /// primeiros dígitos repetem o <see cref="CstIbsCbs"/>). Tabela oficial no Portal
    /// Nacional da NFC-e (anexo III da NT 2025.002) — atualizada com frequência em 2026,
    /// por isso não validamos a lista fechada de códigos aqui, só o formato.</summary>
    public string? CClassTrib { get; set; }

    /// <summary>CST do Imposto Seletivo — 3 dígitos. Só se aplica a produtos do "IS"
    /// (cigarros, bebidas alcoólicas, veículos, etc.) — normalmente vazio para tintas.</summary>
    public string? CstIS { get; set; }

    /// <summary>cClassTrib específico do Imposto Seletivo — 6 dígitos, mesma regra do
    /// <see cref="CClassTrib"/> acima.</summary>
    public string? CClassTribIS { get; set; }

    /// <summary>Alíquota do Imposto Seletivo (%) — só relevante quando <see cref="CstIS"/>
    /// está preenchido.</summary>
    public decimal? AliquotaIS { get; set; }

    // --- Exceções à Regra Geral do IBS Municipal (ConfiguracaoFiscalEmpresa) ---
    // A alíquota principal do IBS Municipal vem da Regra Geral da empresa; estes campos
    // só se preenchem quando ESTE produto tem tratamento diferente (diferimento parcial
    // ou redução de alíquota) — equivalente ao "Cadastro de Regra Tributária" do Olist.
    public decimal? AliquotaIbsMunicipioDiferimento { get; set; }
    public decimal? AliquotaIbsMunicipioReducao { get; set; }

    public DateTime? AtualizadoEm { get; set; }

    public Produto Produto { get; set; } = null!;
}
