using System.Diagnostics;
using System.Text;

namespace ImperialColors.Infrastructure.Services.Backup;

public static class PgDumpExecutor
{
    public static async Task ExecutarAsync(
        string pgDumpPath,
        string host,
        string porta,
        string usuario,
        string senha,
        string banco,
        string arquivoSaida,
        CancellationToken cancellationToken = default)
    {
        var argumentos = new StringBuilder()
            .Append("-h ").Append(EscaparArgumento(host)).Append(' ')
            .Append("-p ").Append(EscaparArgumento(porta)).Append(' ')
            .Append("-U ").Append(EscaparArgumento(usuario)).Append(' ')
            .Append("-d ").Append(EscaparArgumento(banco)).Append(' ')
            .Append("-F p ")
            .Append("--no-owner --no-acl ")
            .Append("-f ").Append(EscaparArgumento(arquivoSaida))
            .ToString();

        var psi = new ProcessStartInfo
        {
            FileName = pgDumpPath,
            Arguments = argumentos,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        psi.Environment["PGPASSWORD"] = senha;

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
            throw new InvalidOperationException("Não foi possível iniciar o pg_dump.");

        var erro = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"pg_dump retornou código {process.ExitCode}: {erro.Trim()}");

        if (!File.Exists(arquivoSaida))
            throw new InvalidOperationException("pg_dump concluiu, mas o arquivo de saída não foi encontrado.");
    }

    public static string? LocalizarPgDump(string? caminhoConfigurado)
    {
        if (!string.IsNullOrWhiteSpace(caminhoConfigurado) && File.Exists(caminhoConfigurado))
            return caminhoConfigurado;

        var noPath = LocalizarNoPath("pg_dump");
        if (noPath is not null)
            return noPath;

        foreach (var raiz in ObterPastasProgramFiles())
        {
            var postgresRoot = Path.Combine(raiz, "PostgreSQL");
            if (!Directory.Exists(postgresRoot))
                continue;

            foreach (var versao in Directory.GetDirectories(postgresRoot).OrderDescending(StringComparer.OrdinalIgnoreCase))
            {
                var candidato = Path.Combine(versao, "bin", "pg_dump.exe");
                if (File.Exists(candidato))
                    return candidato;
            }
        }

        return null;
    }

    private static IEnumerable<string> ObterPastasProgramFiles()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            yield return programFilesX86;
    }

    private static string? LocalizarNoPath(string executavel)
    {
        var extensoes = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat", "" }
            : new[] { "" };

        var paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pasta in paths)
        {
            foreach (var ext in extensoes)
            {
                var candidato = Path.Combine(pasta, executavel + ext);
                if (File.Exists(candidato))
                    return candidato;
            }
        }

        return null;
    }

    private static string EscaparArgumento(string valor)
        => valor.Contains(' ') || valor.Contains('"')
            ? $"\"{valor.Replace("\"", "\\\"")}\""
            : valor;
}
