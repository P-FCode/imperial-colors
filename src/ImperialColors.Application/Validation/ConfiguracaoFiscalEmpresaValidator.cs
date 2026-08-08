using System.Text.RegularExpressions;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Validation;

/// <summary>
/// Valida o endereço fiscal estruturado do emitente e os demais campos que alimentam a
/// NF-e/NFC-e (série, CSC/idCSC). Todos os campos são opcionais até serem preenchidos —
/// mas, uma vez preenchidos, seguem o formato exigido pelo layout oficial.
/// </summary>
public static class ConfiguracaoFiscalEmpresaValidator
{
    private static readonly Regex ApenasDigitos = new("^[0-9]+$", RegexOptions.Compiled);

    private static readonly HashSet<string> UfsValidas = new(StringComparer.OrdinalIgnoreCase)
    {
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    };

    public static void Validar(ConfiguracaoFiscalEmpresaDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Cep) &&
            (dto.Cep.Length != 8 || !ApenasDigitos.IsMatch(dto.Cep)))
            throw new DomainException("CEP deve conter exatamente 8 dígitos numéricos, sem hífen.");

        if (!string.IsNullOrWhiteSpace(dto.CodigoMunicipioIbge) &&
            (dto.CodigoMunicipioIbge.Length != 7 || !ApenasDigitos.IsMatch(dto.CodigoMunicipioIbge)))
            throw new DomainException("Código IBGE do município deve conter exatamente 7 dígitos numéricos.");

        if (!string.IsNullOrWhiteSpace(dto.Uf) && !UfsValidas.Contains(dto.Uf.Trim()))
            throw new DomainException($"UF '{dto.Uf}' inválida. Use a sigla de 2 letras (ex.: PR, SP).");

        if (!string.IsNullOrWhiteSpace(dto.Serie))
        {
            if (dto.Serie.Length is < 1 or > 3 || !ApenasDigitos.IsMatch(dto.Serie))
                throw new DomainException("Série fiscal deve conter de 1 a 3 dígitos numéricos.");

            if (int.Parse(dto.Serie) is < 0 or > 889)
                throw new DomainException("Série fiscal deve estar entre 0 e 889.");
        }

        ValidarCscPar(dto.IdCscHomologacao, dto.CscHomologacao, "Homologação");
        ValidarCscPar(dto.IdCscProducao, dto.CscProducao, "Produção");

        ValidarPercentual(dto.AliquotaIbsUfPadrao, "Alíquota padrão do IBS (UF)");
        ValidarPercentual(dto.AliquotaIbsMunicipioPadrao, "Alíquota padrão do IBS (Município)");
        ValidarPercentual(dto.AliquotaCbsPadrao, "Alíquota padrão da CBS");
        ValidarCstCClassTrib(dto.CstIbsCbsPadrao, dto.CClassTribPadrao);

        if (!string.IsNullOrWhiteSpace(dto.EmailPadraoEnvioNotas) && !dto.EmailPadraoEnvioNotas.Contains('@'))
            throw new DomainException("E-mail padrão de envio de notas inválido.");

        ValidarInscricoesSubstituto(dto.InscricoesSubstitutoTributario);
    }

    private static void ValidarCstCClassTrib(string? cst, string? cClassTrib)
    {
        if (!string.IsNullOrWhiteSpace(cst) && (cst.Length != 3 || !ApenasDigitos.IsMatch(cst)))
            throw new DomainException("CST do IBS/CBS (Regra Geral) deve conter exatamente 3 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(cClassTrib))
            return;

        if (cClassTrib.Length != 6 || !ApenasDigitos.IsMatch(cClassTrib))
            throw new DomainException("cClassTrib (Regra Geral) deve conter exatamente 6 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(cst))
            throw new DomainException("Informe também o CST do IBS/CBS (Regra Geral) — os 3 primeiros dígitos do cClassTrib devem ser iguais ao CST.");

        if (!cClassTrib.StartsWith(cst, StringComparison.Ordinal))
            throw new DomainException(
                $"cClassTrib da Regra Geral inconsistente: os 3 primeiros dígitos ('{cClassTrib[..3]}') devem ser iguais ao CST informado ('{cst}').");
    }

    private static void ValidarInscricoesSubstituto(List<InscricaoEstadualSubstitutoDto> inscricoes)
    {
        var ufsVistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var inscricao in inscricoes)
        {
            if (string.IsNullOrWhiteSpace(inscricao.Uf) || !UfsValidas.Contains(inscricao.Uf.Trim()))
                throw new DomainException($"UF '{inscricao.Uf}' inválida na lista de Inscrições de Substituto Tributário.");

            if (string.IsNullOrWhiteSpace(inscricao.InscricaoEstadual))
                throw new DomainException($"Informe a Inscrição Estadual de Substituto Tributário para {inscricao.Uf}.");

            if (!ufsVistas.Add(inscricao.Uf.Trim().ToUpperInvariant()))
                throw new DomainException($"UF '{inscricao.Uf}' duplicada na lista de Inscrições de Substituto Tributário — cada UF só pode aparecer uma vez.");
        }
    }

    /// <summary>
    /// idCSC e CSC sempre andam juntos — preencher um sem o outro é um cadastro pela
    /// metade que só vai ser descoberto na hora de gerar o QR Code da NFC-e.
    /// </summary>
    private static void ValidarCscPar(string? idCsc, string? csc, string ambiente)
    {
        var temId = !string.IsNullOrWhiteSpace(idCsc);
        var temCsc = !string.IsNullOrWhiteSpace(csc);

        if (temId != temCsc)
            throw new DomainException(
                $"idCSC e CSC de {ambiente} precisam ser preenchidos juntos — falta o outro campo.");
    }

    private static void ValidarPercentual(decimal? valor, string rotulo)
    {
        if (valor is null)
            return;

        if (valor.Value < 0 || valor.Value > 100)
            throw new DomainException($"{rotulo} deve estar entre 0 e 100.");
    }
}
