using System.Text.RegularExpressions;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Validation;

/// <summary>
/// Valida o cadastro de tributação de um produto: formato de NCM/CEST/GTIN, faixa das
/// alíquotas e a regra CST × CSOSN mutuamente exclusivos conforme o regime tributário
/// configurado da empresa. Objetivo: pegar o erro no cadastro, não na hora de emitir a nota.
/// </summary>
public static class TributacaoProdutoValidator
{
    private static readonly Regex ApenasDigitos = new("^[0-9]+$", RegexOptions.Compiled);

    public static void Validar(TributacaoProdutoDto dto, RegimeTributario regime)
    {
        ValidarNcm(dto.Ncm);
        ValidarCest(dto.Cest);
        ValidarIcms(dto, regime);
        ValidarPis(dto);
        ValidarCofins(dto);
        ValidarIpi(dto);
        ValidarUnidadeTributavel(dto);
        ValidarGtinTributavel(dto.GtinTributavel);
        ValidarCfop(dto.CfopDentroEstado, "5", "venda dentro do estado");
        ValidarCfop(dto.CfopForaEstado, "6", "venda para outro estado");
        ValidarReformaTributaria(dto.CstIbsCbs, dto.CClassTrib, "IBS/CBS");
        ValidarReformaTributaria(dto.CstIS, dto.CClassTribIS, "Imposto Seletivo");
        ValidarPercentual(dto.AliquotaIS, "Alíquota do Imposto Seletivo", 0, 100);
        ValidarPercentual(dto.AliquotaIbsMunicipioDiferimento, "Alíquota de diferimento do IBS Municipal", 0, 100);
        ValidarPercentual(dto.AliquotaIbsMunicipioReducao, "Redução de alíquota do IBS Municipal", 0, 100);

        if (dto.ValorIpiFixo is < 0)
            throw new DomainException("Valor fixo do IPI não pode ser negativo.");

        if (!string.IsNullOrWhiteSpace(dto.ExTipi) &&
            (dto.ExTipi.Length != 3 || !ApenasDigitos.IsMatch(dto.ExTipi)))
            throw new DomainException("EX TIPI deve conter exatamente 3 dígitos numéricos.");
    }

    private static void ValidarNcm(string? ncm)
    {
        if (string.IsNullOrWhiteSpace(ncm))
            return;

        if (ncm.Length != 8 || !ApenasDigitos.IsMatch(ncm))
            throw new DomainException("NCM deve conter exatamente 8 dígitos numéricos.");
    }

    private static void ValidarCest(string? cest)
    {
        if (string.IsNullOrWhiteSpace(cest))
            return;

        if (cest.Length != 7 || !ApenasDigitos.IsMatch(cest))
            throw new DomainException("CEST deve conter exatamente 7 dígitos numéricos.");
    }

    private static void ValidarIcms(TributacaoProdutoDto dto, RegimeTributario regime)
    {
        var temCst = !string.IsNullOrWhiteSpace(dto.CstIcms);
        var temCsosn = !string.IsNullOrWhiteSpace(dto.CsosnIcms);

        if (temCst && temCsosn)
            throw new DomainException(
                "Preencha apenas CST ou apenas CSOSN do ICMS, nunca os dois — eles são mutuamente exclusivos.");

        // CRT 1 (Simples Nacional), 2 (Simples excesso de sublimite) e 4 (MEI) usam
        // CSOSN; só o CRT 3 (Regime Normal — Lucro Presumido/Real) usa CST.
        var usaCsosn = regime is RegimeTributario.SimplesNacional or RegimeTributario.Mei;

        if (usaCsosn)
        {
            if (temCst)
                throw new DomainException(
                    "A empresa está no regime Simples Nacional/MEI: use CSOSN para o ICMS, não CST.");

            if (temCsosn && !CodigosFiscais.CsosnValidos.Contains(dto.CsosnIcms!))
                throw new DomainException(
                    $"CSOSN '{dto.CsosnIcms}' inválido. Códigos aceitos: {string.Join(", ", CodigosFiscais.CsosnValidos.OrderBy(c => c))}.");
        }
        else
        {
            if (temCsosn)
                throw new DomainException(
                    "A empresa está no regime Lucro Presumido/Real: use CST para o ICMS, não CSOSN.");

            if (temCst && !CodigosFiscais.CstIcmsValidos.Contains(dto.CstIcms!))
                throw new DomainException(
                    $"CST de ICMS '{dto.CstIcms}' inválido. Códigos aceitos: {string.Join(", ", CodigosFiscais.CstIcmsValidos.OrderBy(c => c))}.");
        }

        ValidarPercentual(dto.AliquotaIcms, "Alíquota de ICMS", 0, 100);
        ValidarPercentual(dto.AliquotaIcmsSt, "Alíquota de ICMS-ST", 0, 100);
        ValidarPercentual(dto.Mva, "MVA/IVA-ST", 0, 999);
        ValidarPercentual(dto.ReducaoBaseCalculo, "Redução de base de cálculo", 0, 100);
    }

    private static void ValidarPis(TributacaoProdutoDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CstPis) &&
            (dto.CstPis.Length != 2 || !ApenasDigitos.IsMatch(dto.CstPis)))
            throw new DomainException("CST de PIS deve conter exatamente 2 dígitos numéricos.");

        ValidarPercentual(dto.AliquotaPis, "Alíquota de PIS", 0, 100);
    }

    private static void ValidarCofins(TributacaoProdutoDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CstCofins) &&
            (dto.CstCofins.Length != 2 || !ApenasDigitos.IsMatch(dto.CstCofins)))
            throw new DomainException("CST de COFINS deve conter exatamente 2 dígitos numéricos.");

        ValidarPercentual(dto.AliquotaCofins, "Alíquota de COFINS", 0, 100);
    }

    private static void ValidarIpi(TributacaoProdutoDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CstIpi))
        {
            if (dto.CstIpi.Length != 2 || !ApenasDigitos.IsMatch(dto.CstIpi))
                throw new DomainException("CST de IPI deve conter exatamente 2 dígitos numéricos.");

            if (!CodigosFiscais.CstIpiValidos.Contains(dto.CstIpi))
                throw new DomainException(
                    $"CST de IPI '{dto.CstIpi}' inválido. Códigos aceitos: {string.Join(", ", CodigosFiscais.CstIpiValidos.OrderBy(c => c))}.");
        }

        if (!string.IsNullOrWhiteSpace(dto.CodigoEnquadramentoIpi) &&
            (dto.CodigoEnquadramentoIpi.Length != 3 || !ApenasDigitos.IsMatch(dto.CodigoEnquadramentoIpi)))
            throw new DomainException("Código de enquadramento do IPI deve conter exatamente 3 dígitos numéricos.");

        ValidarPercentual(dto.AliquotaIpi, "Alíquota de IPI", 0, 100);
    }

    private static void ValidarUnidadeTributavel(TributacaoProdutoDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.UnidadeTributavel) && dto.UnidadeTributavel.Trim().Length > 10)
            throw new DomainException("Unidade tributável inválida.");

        if (dto.FatorConversao is <= 0)
            throw new DomainException("Fator de conversão da unidade tributável deve ser maior que zero.");
    }

    private static void ValidarGtinTributavel(string? gtin)
    {
        if (string.IsNullOrWhiteSpace(gtin))
            return;

        var normalizado = gtin.Trim();
        if (string.Equals(normalizado, "SEM GTIN", StringComparison.OrdinalIgnoreCase))
            return;

        if (!ApenasDigitos.IsMatch(normalizado) ||
            (normalizado.Length != 8 && normalizado.Length != 12 &&
             normalizado.Length != 13 && normalizado.Length != 14))
            throw new DomainException(
                "GTIN tributável deve ter 8, 12, 13 ou 14 dígitos numéricos, ou o texto 'SEM GTIN'.");
    }

    /// <summary>
    /// CFOP de venda: 4 dígitos, sempre começando pelo dígito de saída correto
    /// (5 = dentro do estado, 6 = fora do estado — grupos 1/2/3/7 não fazem sentido
    /// aqui porque são de entrada ou exterior). Essa regra é a mesma que a API de
    /// emissão aplica antes de transmitir (erros CFOP_INTERNO_INVALIDO /
    /// CFOP_INTERESTADUAL_INVALIDO) — pegar aqui evita descobrir só na hora de emitir.
    /// </summary>
    private static void ValidarCfop(string? cfop, string digitoEsperado, string rotulo)
    {
        if (string.IsNullOrWhiteSpace(cfop))
            return;

        if (cfop.Length != 4 || !ApenasDigitos.IsMatch(cfop))
            throw new DomainException($"CFOP de {rotulo} deve conter exatamente 4 dígitos numéricos.");

        if (!cfop.StartsWith(digitoEsperado, StringComparison.Ordinal))
            throw new DomainException(
                $"CFOP de {rotulo} deve começar com {digitoEsperado} (ex.: {digitoEsperado}102) — '{cfop}' não é um CFOP de saída válido para esse caso.");
    }

    /// <summary>
    /// Valida CST + cClassTrib da Reforma Tributária (IBS/CBS ou Imposto Seletivo — mesma
    /// estrutura para os dois): CST com 3 dígitos, cClassTrib com 6 dígitos no formato
    /// "CSTYYY" (os 3 primeiros dígitos do cClassTrib repetem o CST). Não valida contra a
    /// lista fechada de códigos — a tabela oficial (Portal Nacional da NFC-e, anexo III da
    /// NT 2025.002) é atualizada com frequência em 2026 e travar aqui rejeitaria códigos
    /// novos e legítimos assim que o Comitê Gestor publicasse revisões.
    /// </summary>
    private static void ValidarReformaTributaria(string? cst, string? cClassTrib, string rotulo)
    {
        if (!string.IsNullOrWhiteSpace(cst) && (cst.Length != 3 || !ApenasDigitos.IsMatch(cst)))
            throw new DomainException($"CST de {rotulo} deve conter exatamente 3 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(cClassTrib))
            return;

        if (cClassTrib.Length != 6 || !ApenasDigitos.IsMatch(cClassTrib))
            throw new DomainException($"cClassTrib de {rotulo} deve conter exatamente 6 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(cst))
            throw new DomainException(
                $"Informe também o CST de {rotulo} — os 3 primeiros dígitos do cClassTrib devem ser iguais ao CST.");

        if (!cClassTrib.StartsWith(cst, StringComparison.Ordinal))
            throw new DomainException(
                $"cClassTrib de {rotulo} inconsistente: os 3 primeiros dígitos ('{cClassTrib[..3]}') devem ser iguais ao CST informado ('{cst}').");
    }

    private static void ValidarPercentual(decimal? valor, string rotulo, decimal min, decimal max)
    {
        if (valor is null)
            return;

        if (valor.Value < min || valor.Value > max)
            throw new DomainException($"{rotulo} deve estar entre {min} e {max}.");
    }
}
