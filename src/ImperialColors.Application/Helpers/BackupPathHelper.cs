using System.Globalization;

namespace ImperialColors.Application.Helpers;

public static class BackupPathHelper
{
    private static readonly CultureInfo CulturaPtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string MontarPastaDestino(string diretorioRaiz, DateTime dataExecucao)
    {
        var mesAno = $"{CulturaPtBr.DateTimeFormat.GetMonthName(dataExecucao.Month).ToLower()}-{dataExecucao.Year}";
        var pastaDia = dataExecucao.ToString("dd-MM-yyyy", CulturaPtBr);
        return Path.Combine(diretorioRaiz, mesAno, pastaDia);
    }

    public static string MontarNomeArquivoSql(string prefixoEmpresa, DateTime dataExecucao)
    {
        var prefixo = SanitizarPrefixo(prefixoEmpresa);
        return $"backup_{prefixo}_{dataExecucao:dd_MM_yyyy}.sql";
    }

    public static string SanitizarPrefixo(string prefixo)
    {
        if (string.IsNullOrWhiteSpace(prefixo))
            return "imperial";

        var chars = prefixo.Trim().ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray();

        return chars.Length == 0 ? "imperial" : new string(chars);
    }
}
