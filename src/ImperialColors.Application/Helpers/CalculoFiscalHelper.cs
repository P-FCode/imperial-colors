using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Calcula ICMS/PIS/COFINS/IBS/CBS de um item de venda a partir da tributação cadastrada
/// do produto — função pura, sem acesso a banco, para poder testar com números conhecidos.
///
/// Cobre os casos mais comuns de uma loja de varejo:
/// - Simples Nacional (CSOSN): ICMS embutido no preço, não destaca vBC/vICMS na nota —
///   inclusive CSOSN 101/201 (que dão direito a crédito para o comprador, mas isso é um
///   campo à parte, não altera o valor da nota).
/// - Regime Normal (CST) com tributação integral (00/20/51/90): calcula vBC × alíquota.
/// - Isenção/não tributação/suspensão (CST 40/41/50): sem valor.
/// - PIS/COFINS com alíquota (CST 01/02): calcula vBC × alíquota. Não tributado (04-09): sem valor.
///
/// O que NÃO calcula (gera aviso em vez de arriscar um número errado):
/// - Substituição Tributária (CST ICMS 10/60) — precisa de MVA/base própria, regra à parte.
/// - Qualquer CST fora da lista acima — melhor sinalizar do que inventar uma fórmula.
/// </summary>
public static class CalculoFiscalHelper
{
    private static readonly HashSet<string> CstIcmsTributacaoIntegral = ["00", "20", "51", "90"];
    private static readonly HashSet<string> CstIcmsSemDestaque = ["40", "41", "50"];
    private static readonly HashSet<string> CstIcmsSubstituicaoTributaria = ["10", "60"];

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

        CalcularIcms(resultado, cstIcms, csosnIcms, aliquotaIcms, valorItem);
        CalcularPisCofins(resultado, "PIS", cstPis, aliquotaPis, valorItem, (r, v) => r.VPis = v);
        CalcularPisCofins(resultado, "COFINS", cstCofins, aliquotaCofins, valorItem, (r, v) => r.VCofins = v);
        CalcularIpi(resultado, cstIpi, aliquotaIpi, valorItem);

        // IBS/CBS — ano-piloto 2026: alíquotas fixas e nacionais, aplicadas sobre a
        // mesma base do item, independente do regime de ICMS.
        resultado.VIbsUf = Arredondar(valorItem * aliquotaIbsUf / 100m);
        resultado.VIbsMunicipio = Arredondar(valorItem * aliquotaIbsMunicipio / 100m);
        resultado.VCbs = Arredondar(valorItem * aliquotaCbs / 100m);

        return resultado;
    }

    private static void CalcularIcms(
        ItemCalculoFiscalDto resultado, string? cstIcms, string? csosnIcms, decimal? aliquotaIcms, decimal valorItem)
    {
        if (!string.IsNullOrWhiteSpace(csosnIcms))
        {
            // Simples Nacional: nenhum CSOSN destaca vICMS na nota — o imposto está
            // embutido no preço de venda, não é somado à parte.
            return;
        }

        if (string.IsNullOrWhiteSpace(cstIcms))
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': sem CST/CSOSN de ICMS cadastrado.");
            return;
        }

        if (CstIcmsTributacaoIntegral.Contains(cstIcms))
        {
            if (!aliquotaIcms.HasValue)
            {
                resultado.Avisos.Add($"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' exige alíquota, mas ela não está cadastrada.");
                return;
            }

            resultado.VIcms = Arredondar(valorItem * aliquotaIcms.Value / 100m);
        }
        else if (CstIcmsSemDestaque.Contains(cstIcms))
        {
            // Isenta/não tributada/suspensão — sem valor a destacar, corretamente.
        }
        else if (CstIcmsSubstituicaoTributaria.Contains(cstIcms))
        {
            resultado.Avisos.Add(
                $"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' é de Substituição Tributária — cálculo de ICMS-ST não implementado, revisar manualmente antes de emitir.");
        }
        else
        {
            resultado.Avisos.Add(
                $"'{resultado.NomeProduto}': CST ICMS '{cstIcms}' não tem regra de cálculo automática cadastrada — revisar manualmente.");
        }
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

    private static void CalcularPisCofins(
        ItemCalculoFiscalDto resultado,
        string rotulo,
        string? cst,
        decimal? aliquota,
        decimal valorItem,
        Action<ItemCalculoFiscalDto, decimal> atribuirValor)
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

            atribuirValor(resultado, Arredondar(valorItem * aliquota.Value / 100m));
        }
        else if (!CstPisCofinsNaoTributado.Contains(cst))
        {
            resultado.Avisos.Add($"'{resultado.NomeProduto}': CST {rotulo} '{cst}' não tem regra de cálculo automática cadastrada — revisar manualmente.");
        }
        // CST em CstPisCofinsNaoTributado: sem valor a destacar, corretamente.
    }

    private static decimal Arredondar(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);
}
