using System.Globalization;
using System.Text;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Gera códigos internos a partir das iniciais do nome (ex.: "Massa Corrida Coral" → MCC001).
/// </summary>
public static class ProdutoCodigoIniciaisHelper
{
    private static readonly HashSet<string> PreposicoesIgnoradas = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "da", "do", "dos", "das", "para", "e", "a", "o", "em", "na", "no", "nas", "nos", "com", "por"
    };

    public static string ExtrairSigla(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return string.Empty;

        var sigla = new StringBuilder();
        foreach (var palavra in nome.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var normalizada = RemoverAcentos(palavra.Trim());
            if (normalizada.Length < 2 || PreposicoesIgnoradas.Contains(normalizada))
                continue;

            sigla.Append(char.ToUpperInvariant(normalizada[0]));
        }

        return sigla.ToString();
    }

    public static string FormatarCodigo(string sigla, int sequencia)
        => $"{sigla.ToUpperInvariant()}{sequencia:D3}";

    public static bool TryExtrairSequencia(string codigo, string siglaEsperada, out int sequencia)
    {
        sequencia = 0;
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(siglaEsperada))
            return false;

        var sigla = siglaEsperada.ToUpperInvariant();
        var codigoUpper = codigo.ToUpperInvariant();
        if (!codigoUpper.StartsWith(sigla, StringComparison.Ordinal) || codigoUpper.Length != sigla.Length + 3)
            return false;

        return int.TryParse(codigoUpper[sigla.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out sequencia)
               && sequencia > 0;
    }

    public static bool EhCodigoPorIniciais(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 4)
            return false;

        if (ProdutoCodigoInternoHelper.EhCodigoSequencialPadrao(codigo))
            return false;

        var parteNumerica = codigo[^3..];
        return int.TryParse(parteNumerica, NumberStyles.None, CultureInfo.InvariantCulture, out var seq) && seq > 0;
    }

    private static string RemoverAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
