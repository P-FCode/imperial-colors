using System.Text.RegularExpressions;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Validation;

/// <summary>
/// Valida o cadastro de uma Natureza de Operação: mesma regra CST × CSOSN mutuamente
/// exclusivos e mesmo formato de CFOP já aplicados no cadastro de produto/categoria —
/// aqui é só a versão "padrão reutilizável" desses campos.
/// </summary>
public static class NaturezaOperacaoValidator
{
    private static readonly Regex ApenasDigitos = new("^[0-9]+$", RegexOptions.Compiled);

    public static void Validar(NaturezaOperacaoDto dto, RegimeTributario regime)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao))
            throw new DomainException("Descrição da natureza de operação é obrigatória.");

        if (dto.Descricao.Trim().Length > 150)
            throw new DomainException("Descrição da natureza de operação deve ter no máximo 150 caracteres.");

        if (!string.IsNullOrWhiteSpace(dto.Serie) && dto.Serie.Trim().Length > 3)
            throw new DomainException("Série deve ter no máximo 3 caracteres.");

        ValidarIcms(dto, regime);
        ValidarCfop(dto.CfopDentroEstado, "5", "venda dentro do estado");
        ValidarCfop(dto.CfopForaEstado, "6", "venda para outro estado");
    }

    private static void ValidarIcms(NaturezaOperacaoDto dto, RegimeTributario regime)
    {
        var temCst = !string.IsNullOrWhiteSpace(dto.CstIcmsPadrao);
        var temCsosn = !string.IsNullOrWhiteSpace(dto.CsosnPadrao);

        if (temCst && temCsosn)
            throw new DomainException(
                "Preencha apenas CST ou apenas CSOSN do ICMS, nunca os dois — eles são mutuamente exclusivos.");

        var usaCsosn = regime is RegimeTributario.SimplesNacional or RegimeTributario.Mei;

        if (usaCsosn)
        {
            if (temCst)
                throw new DomainException(
                    "A empresa está no regime Simples Nacional/MEI: use CSOSN para o ICMS, não CST.");

            if (temCsosn && !CodigosFiscais.CsosnValidos.Contains(dto.CsosnPadrao!))
                throw new DomainException(
                    $"CSOSN '{dto.CsosnPadrao}' inválido. Códigos aceitos: {string.Join(", ", CodigosFiscais.CsosnValidos.OrderBy(c => c))}.");
        }
        else
        {
            if (temCsosn)
                throw new DomainException(
                    "A empresa está no regime Lucro Presumido/Real: use CST para o ICMS, não CSOSN.");

            if (temCst && !CodigosFiscais.CstIcmsValidos.Contains(dto.CstIcmsPadrao!))
                throw new DomainException(
                    $"CST de ICMS '{dto.CstIcmsPadrao}' inválido. Códigos aceitos: {string.Join(", ", CodigosFiscais.CstIcmsValidos.OrderBy(c => c))}.");
        }
    }

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
}
