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

    /// <summary>Script SQL em texto — legível, abre no Bloco de Notas e restaura com
    /// <c>psql</c>. Ver <see cref="MontarNomeArquivoDump"/> para o par comprimido.</summary>
    public static string MontarNomeArquivoSql(string prefixoEmpresa, DateTime dataExecucao)
        => MontarNomeBase(prefixoEmpresa, dataExecucao) + ".sql";

    /// <summary>Formato custom do <c>pg_dump</c> (<c>-F c</c>) — comprimido e restaurável por
    /// tabela com <c>pg_restore</c>. Mesmo nome-base do <c>.sql</c> do mesmo dia, para os dois
    /// ficarem lado a lado na pasta e ser óbvio que são o mesmo backup.</summary>
    public static string MontarNomeArquivoDump(string prefixoEmpresa, DateTime dataExecucao)
        => MontarNomeBase(prefixoEmpresa, dataExecucao) + ".dump";

    private static string MontarNomeBase(string prefixoEmpresa, DateTime dataExecucao)
        => $"backup_{SanitizarPrefixo(prefixoEmpresa)}_{dataExecucao:dd_MM_yyyy}";

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
