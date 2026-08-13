using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Calcula ICMS/ICMS-ST/PIS/COFINS/IBS/CBS de um item de venda a partir da tributação
/// cadastrada do produto — função pura, sem acesso a banco, para poder testar com números
/// conhecidos.
///
/// Cobre os casos mais comuns de uma loja de varejo:
/// - Simples Nacional (CSOSN) sem ST — 101/102/103/300/400/900: ICMS embutido no preço, não
///   destaca vBC/vICMS na nota (101 dá direito a crédito para o comprador, mas isso é um
///   campo à parte, não altera o valor da nota; 900 "Outros" é sinalizado — não é óbvio o
///   que ele carrega, então avisa em vez de arriscar tratar como "sem destaque").
/// - Regime Normal (CST) com tributação integral (00/20/51/90): calcula vBC × alíquota.
/// - Isenção/não tributação/suspensão (CST 40/41/50): sem valor.
/// - PIS/COFINS com alíquota (CST 01/02): calcula vBC × alíquota. Não tributado (04-09): sem valor.
/// - Substituição Tributária "para frente" (CST 10, CSOSN 201/202/203): vBCST = base ×
///   (1 + MVA/100); vICMSST = vBCST × pICMSST − vICMS próprio (CST 10 tem vICMS próprio além
///   do ST; CSOSN não destaca vICMS próprio, então não há o que subtrair).
/// - Substituição Tributária retida anteriormente (CST 60, CSOSN 500): não é cálculo por
///   fórmula de margem — o ICMS-ST já foi recolhido por um elo anterior da cadeia (fabricante/
///   distribuidor). Este sistema declara vICMSSTRet = valor do item × pST informado no
///   cadastro (percentual vindo da nota de compra ou de tabela do fisco estadual), a mesma
///   convenção de "alíquota simples sobre a base do item" já usada no resto do sistema — não
///   o valor exato retido na aquisição original (isso exigiria rastrear ICMS-ST por lote de
///   compra, fora do escopo deste sistema).
///
/// O que NÃO calcula (gera aviso em vez de arriscar um número errado):
/// - Qualquer CST/CSOSN fora da lista acima — melhor sinalizar do que inventar uma fórmula.
/// </summary>
public static class CalculoFiscalHelper
{
    /// <summary>CST de ICMS cujo grupo no XML (ICMS00/ICMS20/ICMS51/ICMS90 do leiauteNFe_v4.00.xsd)
    /// exige <c>vBC</c>/<c>pICMS</c>/<c>vICMS</c> — <c>internal</c> (não <c>private</c>) porque
    /// <see cref="Validation.NotaFiscalValidator"/> precisa da mesma lista para bloquear a
    /// emissão quando falta alíquota cadastrada, em vez de deixar a API rejeitar por XML
    /// incompleto (cStat local "XSD_VALIDATION ... incomplete content ... expected 'pICMS'").</summary>
    internal static readonly HashSet<string> CstIcmsTributacaoIntegral = ["00", "20", "51", "90"];
    private static readonly HashSet<string> CstIcmsSemDestaque = ["40", "41", "50"];

    /// <summary>CST 10 — ICMS-ST "para frente": tem grupo próprio (vBC/pICMS/vICMS, igual
    /// CST 00) MAIS o grupo ST (vBCST/pICMSST/vICMSST/pMVAST). <c>internal</c> porque
    /// <see cref="Validation.NotaFiscalValidator"/> e <see
    /// cref="Infrastructure.Fiscal.NotaFiscalPayloadBuilder"/> (via cópia — camada
    /// Infrastructure não referencia Application) precisam da mesma classificação.</summary>
    internal static readonly HashSet<string> CstIcmsStParaFrente = ["10"];

    /// <summary>CSOSN 201/202/203 — mesmo grupo ST do CST 10, mas SEM grupo próprio (Simples
    /// Nacional nunca destaca vICMS próprio na nota).</summary>
    internal static readonly HashSet<string> CsosnStParaFrente = ["201", "202", "203"];

    /// <summary>CST 60 — ICMS-ST já retido por um elo anterior da cadeia: grupo
    /// vBCSTRet/pST/vICMSSTRet, distinto do grupo "para frente" acima.</summary>
    internal static readonly HashSet<string> CstIcmsStRetido = ["60"];

    /// <summary>CSOSN 500 — equivalente Simples Nacional do CST 60. Regressão real: nota
    /// rejeitada pela SEFAZ com "Nao informada vBCSTRet, pST e vICMSSTRet" porque este
    /// código passava sem nenhum tratamento (nem aqui, nem no validador, nem no payload).</summary>
    internal static readonly HashSet<string> CsosnStRetido = ["500"];

    /// <summary>CSOSN 900 "Outros" — grupo ICMSSN900 aceita várias combinações de campos
    /// dependendo do caso real; não há como saber qual sem o cadastro dizer. Só um aviso
    /// para revisão manual.</summary>
    private static readonly HashSet<string> CsosnOutros = ["900"];

    private static readonly HashSet<string> CstPisCofinsTributado = ["01", "02"];
    private static readonly HashSet<string> CstPisCofinsNaoTributado = ["04", "05", "06", "07", "08", "09"];

    /// <summary>CST de IPI que não geram vIPI/vBC/pIPI na nota — só o CST é enviado ("saída
    /// não-tributada", "imune", "suspensão" etc.). Mesma classificação usada pela API Fiscal
    /// real para decidir entre TIpiTrib e TIpiNT (Fiscal.Shared.Services.JsonToXsdResolverService).</summary>
    internal static readonly HashSet<string> CstIpiNaoTributado = ["01", "02", "03", "04", "05", "51", "52", "53", "54", "55"];

    public static ItemCalculoFiscalDto CalcularItem(
        int produtoId,
        string nomeProduto,
        decimal valorItem,
        string? cstIcms,
        string? csosnIcms,
        decimal? aliquotaIcms,
        decimal? reducaoBaseCalculo,
        decimal? mva,
        decimal? aliquotaIcmsSt,
        decimal? aliquotaIcmsStRetido,
        string? cstPis,
        decimal? aliquotaPis,
        string? cstCofins,
        decimal? aliquotaCofins,
        string? cstIpi,
        decimal? aliquotaIpi,
        decimal aliquotaIbsUf,
        decimal aliquotaIbsMunicipio,
        decimal aliquotaCbs)
    {
        var resultado = new ItemCalculoFiscalDto
        {
            ProdutoId = produtoId,
            NomeProduto = nomeProduto,
            ValorItem = valorItem
        };

        CalcularIcms(resultado, cstIcms, csosnIcms, aliquotaIcms, reducaoBaseCalculo,
            mva, aliquotaIcmsSt, aliquotaIcmsStRetido, valorItem);
        CalcularPisCofins(resultado, "PIS", cstPis, aliquotaPis, valorItem,
            (r, vbc, v) => { r.VBcPis = vbc; r.VPis = v; });
        CalcularPisCofins(resultado, "COFINS", cstCofins, aliquotaCofins, valorItem,
            (r, vbc, v) => { r.VBcCofins = vbc; r.VCofins = v; });
        CalcularIpi(resultado, cstIpi, aliquotaIpi, valorItem);

        // IBS/CBS — ano-piloto 2026: alíquotas fixas e nacionais, aplicadas sobre a
        // mesma base do item, independente do regime de ICMS.
        resultado.VIbsUf = Arredondar(valorItem * aliquotaIbsUf / 100m);
        resultado.VIbsMunicipio = Arredondar(valorItem * aliquotaIbsMunicipio / 100m);
        resultado.VCbs = Arredondar(valorItem * aliquotaCbs / 100m);

        return resultado;
    }

    private static void CalcularIcms(
        ItemCalculoFiscalDto resultado, string? cstIcms, string? csosnIcms, decimal? aliquotaIcms,
        decimal? reducaoBaseCalculo, decimal? mva, decimal? aliquotaIcmsSt, decimal? aliquotaIcmsStRetido,
        decimal valorItem)
    {
        if (!string.IsNullOrWhiteSpace(csosnIcms))
        {
            if (CsosnStParaFrente.Contains(csosnIcms))
            {
                // Simples Nacional não destaca ICMS próprio — só o grupo ST.
                CalcularIcmsStParaFrente(resultado, csosnIcms, mva, aliquotaIcmsSt, vIcmsProprio: 0m, valorItem);
            }
            else if (CsosnStRetido.Contains(csosnIcms))
            {
                CalcularIcmsStRetido(resultado, csosnIcms, aliquotaIcmsStRetido, valorItem);
            }
            else if (CsosnOutros.Contains(csosnIcms))
            {
                resultado.Avisos.Add(
                    $"'{resultado.NomeProduto}': CSOSN '{csosnIcms}' (Outros) não tem regra de cálculo automática cadastrada — revisar manualmente.");
            }

            // Nos demais CSOSN (101/102/103/300/400) o ICMS está embutido no preço de venda —
            // não é somado à parte, não há vBC/vICMS a destacar.
            return;
        }

        if (string.IsNullOrWhiteSpace(cstIcms))
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': sem CST/CSOSN de ICMS cadastrado.");
            return;
        }

        if (CstIcmsTributacaoIntegral.Contains(cstIcms))
        {
            // A base do ICMS é sempre declarada (vBC é obrigatório nos grupos ICMS00/20/51/90),
            // mesmo sem alíquota cadastrada — quem bloqueia a emissão nesse caso é o
            // NotaFiscalValidator. A redução de base (pRedBC, típica do CST 20) entra AQUI:
            // antes disso o imposto era calculado sobre o valor cheio do item, destacando
            // ICMS a maior justamente nos produtos com base reduzida.
            resultado.VBcIcms = Arredondar(AplicarReducaoBase(valorItem, reducaoBaseCalculo));

            if (!aliquotaIcms.HasValue)
            {
                resultado.Avisos.Add($"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' exige alíquota, mas ela não está cadastrada.");
                return;
            }

            resultado.VIcms = Arredondar(resultado.VBcIcms.Value * aliquotaIcms.Value / 100m);
        }
        else if (CstIcmsSemDestaque.Contains(cstIcms))
        {
            // Isenta/não tributada/suspensão — sem valor a destacar, corretamente.
        }
        else if (CstIcmsStParaFrente.Contains(cstIcms))
        {
            // CST 10 = grupo próprio (igual CST 00, calculado acima do jeito normal) + grupo
            // ST por cima. Calcula o "próprio" primeiro para poder deduzi-lo do ST.
            resultado.VBcIcms = Arredondar(AplicarReducaoBase(valorItem, reducaoBaseCalculo));

            if (!aliquotaIcms.HasValue)
            {
                resultado.Avisos.Add($"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' exige alíquota do ICMS próprio, mas ela não está cadastrada.");
                return;
            }

            resultado.VIcms = Arredondar(resultado.VBcIcms.Value * aliquotaIcms.Value / 100m);
            CalcularIcmsStParaFrente(resultado, cstIcms, mva, aliquotaIcmsSt, resultado.VIcms, valorItem);
        }
        else if (CstIcmsStRetido.Contains(cstIcms))
        {
            CalcularIcmsStRetido(resultado, cstIcms, aliquotaIcmsStRetido, valorItem);
        }
        else
        {
            resultado.Avisos.Add(
                $"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' não tem regra de cálculo automática cadastrada — revisar manualmente.");
        }
    }

    /// <summary>ICMS-ST "para frente" (CST 10 / CSOSN 201/202/203): vBCST = valor do item ×
    /// (1 + MVA/100); vICMSST = vBCST × pICMSST − ICMS próprio (0 quando o código não destaca
    /// ICMS próprio, caso do CSOSN). Resultado nunca fica negativo — se a dedução do ICMS
    /// próprio superar o ST bruto (MVA/alíquota mal cadastrados), zera em vez de destacar um
    /// imposto negativo, que a SEFAZ rejeitaria de qualquer forma.</summary>
    private static void CalcularIcmsStParaFrente(
        ItemCalculoFiscalDto resultado, string codigo, decimal? mva, decimal? aliquotaIcmsSt,
        decimal vIcmsProprio, decimal valorItem)
    {
        if (!mva.HasValue || !aliquotaIcmsSt.HasValue)
        {
            resultado.Avisos.Add(
                $"'{resultado.NomeProduto}': CST/CSOSN '{codigo}' exige MVA e Alíquota de ICMS-ST cadastrados para calcular a Substituição Tributária, mas faltam.");
            return;
        }

        var vBcSt = Arredondar(valorItem * (100m + mva.Value) / 100m);
        var vIcmsStBruto = Arredondar(vBcSt * aliquotaIcmsSt.Value / 100m);

        resultado.VBcIcmsSt = vBcSt;
        resultado.VIcmsSt = Math.Max(0m, vIcmsStBruto - vIcmsProprio);
    }

    /// <summary>ICMS-ST retido anteriormente (CST 60 / CSOSN 500): vBCSTRet = valor do item
    /// (mesma convenção "base ad-valorem simples" usada para BaseIcms/BaseIbsCbs/BaseIpi);
    /// vICMSSTRet = vBCSTRet × pST informado no cadastro.</summary>
    private static void CalcularIcmsStRetido(
        ItemCalculoFiscalDto resultado, string codigo, decimal? aliquotaIcmsStRetido, decimal valorItem)
    {
        if (!aliquotaIcmsStRetido.HasValue)
        {
            resultado.Avisos.Add(
                $"'{resultado.NomeProduto}': CST/CSOSN '{codigo}' exige a Alíquota de ICMS-ST Retido (pST) cadastrada, mas ela não está.");
            return;
        }

        var vBcStRet = Arredondar(valorItem);
        resultado.VBcIcmsStRetido = vBcStRet;
        resultado.VIcmsStRetido = Arredondar(vBcStRet * aliquotaIcmsStRetido.Value / 100m);
    }

    /// <summary>
    /// Ao contrário de ICMS/PIS/COFINS (obrigatórios em todo item da NF-e), IPI é
    /// genuinamente opcional — a maioria dos produtos de uma loja de varejo não é sujeita a
    /// IPI, então <paramref name="cstIpi"/> vazio não gera aviso (é o caso comum, não uma
    /// pendência de cadastro). Só avisa quando o CST está preenchido, indica tributação, e
    /// falta a alíquota para calcular.
    /// </summary>
    private static void CalcularIpi(ItemCalculoFiscalDto resultado, string? cstIpi, decimal? aliquotaIpi, decimal valorItem)
    {
        if (string.IsNullOrWhiteSpace(cstIpi) || CstIpiNaoTributado.Contains(cstIpi))
            return;

        if (!aliquotaIpi.HasValue)
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': CST IPI '{cstIpi}' exige alíquota, mas ela não está cadastrada.");
            return;
        }

        resultado.VIpi = Arredondar(valorItem * aliquotaIpi.Value / 100m);
    }

    /// <param name="atribuirValores">Recebe (base, valor) — a base é tão obrigatória quanto o
    /// valor no XML: o grupo PISAliq/COFINSAliq exige vBC + pPIS + vPIS juntos.</param>
    private static void CalcularPisCofins(
        ItemCalculoFiscalDto resultado,
        string rotulo,
        string? cst,
        decimal? aliquota,
        decimal valorItem,
        Action<ItemCalculoFiscalDto, decimal, decimal> atribuirValores)
    {
        if (string.IsNullOrWhiteSpace(cst))
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': sem CST de {rotulo} cadastrado.");
            return;
        }

        if (CstPisCofinsTributado.Contains(cst))
        {
            if (!aliquota.HasValue)
            {
                resultado.Avisos.Add($"'{resultado.NomeProduto}': CST {rotulo} '{cst}' exige alíquota, mas ela não está cadastrada.");
                return;
            }

            var baseCalculo = Arredondar(valorItem);
            atribuirValores(resultado, baseCalculo, Arredondar(baseCalculo * aliquota.Value / 100m));
        }
        else if (!CstPisCofinsNaoTributado.Contains(cst))
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': CST {rotulo} '{cst}' não tem regra de cálculo automática cadastrada — revisar manualmente.");
        }
        // CST em CstPisCofinsNaoTributado: sem valor a destacar, corretamente.
    }

    /// <summary>Aplica o pRedBC do cadastro sobre o valor do item. Percentuais fora de
    /// 0–100 são ignorados (tratados como "sem redução") em vez de gerar uma base negativa
    /// ou maior que o próprio item — TributacaoProdutoValidator já barra esse cadastro, isto
    /// é só a rede de segurança para dados legados no banco.</summary>
    internal static decimal AplicarReducaoBase(decimal valorItem, decimal? reducaoBaseCalculo)
        => reducaoBaseCalculo is > 0 and <= 100
            ? valorItem * (100m - reducaoBaseCalculo.Value) / 100m
            : valorItem;

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
