using System.Diagnostics;
using System.Text;

namespace ImperialColors.Infrastructure.Services.Backup;

/// <summary>
/// Backup do banco em dois formatos a partir de UMA leitura: o <c>pg_dump</c> gera o
/// <c>.dump</c> (formato custom) e o <c>pg_restore</c> converte esse arquivo no <c>.sql</c>,
/// sem se conectar a banco nenhum.
///
/// Por que converter em vez de rodar o <c>pg_dump</c> duas vezes:
/// <list type="bullet">
/// <item>Os dois arquivos são o MESMO instante do banco. Dois <c>pg_dump</c> seguidos pegariam
/// momentos diferentes se uma venda entrasse no meio, e na hora de restaurar ninguém saberia
/// qual dos dois é "o" backup do dia.</item>
/// <item>O banco é lido uma vez só — o <c>pg_dump</c> é a parte pesada.</item>
/// <item>A conversão prova que o <c>.dump</c> é legível. Um arquivo corrompido falha aqui,
/// no dia em que foi gerado, e não no dia em que alguém precisar dele.</item>
/// </list>
/// </summary>
public static class PgDumpExecutor
{
    /// <summary><c>pg_dump -F c</c>: comprimido e restaurável por tabela.</summary>
    public static Task ExportarAsync(
        string pgDumpPath,
        string host,
        string porta,
        string usuario,
        string senha,
        string banco,
        string arquivoDump,
        CancellationToken cancellationToken = default)
    {
        var argumentos = new StringBuilder()
            .Append("-h ").Append(EscaparArgumento(host)).Append(' ')
            .Append("-p ").Append(EscaparArgumento(porta)).Append(' ')
            .Append("-U ").Append(EscaparArgumento(usuario)).Append(' ')
            .Append("-d ").Append(EscaparArgumento(banco)).Append(' ')
            .Append("-F c ")
            .Append("--no-owner --no-acl ")
            .Append("-f ").Append(EscaparArgumento(arquivoDump))
            .ToString();

        return ExecutarProcessoAsync(pgDumpPath, argumentos, senha, "pg_dump", arquivoDump, cancellationToken);
    }

    /// <summary>
    /// <c>pg_restore</c> sem <c>-d</c> não restaura nada: escreve em <paramref name="arquivoSql"/>
    /// o script SQL equivalente ao conteúdo do <c>.dump</c> — o mesmo texto que um
    /// <c>pg_dump -F p</c> teria gerado. Não precisa de senha nem de conexão.
    /// </summary>
    public static Task ConverterParaSqlAsync(
        string pgRestorePath,
        string arquivoDump,
        string arquivoSql,
        CancellationToken cancellationToken = default)
    {
        var argumentos = new StringBuilder()
            .Append("--no-owner --no-acl ")
            .Append("-f ").Append(EscaparArgumento(arquivoSql)).Append(' ')
            .Append(EscaparArgumento(arquivoDump))
            .ToString();

        return ExecutarProcessoAsync(pgRestorePath, argumentos, senha: null, "pg_restore", arquivoSql, cancellationToken);
    }

    private static async Task ExecutarProcessoAsync(
        string executavel,
        string argumentos,
        string? senha,
        string nomeUtilitario,
        string arquivoSaida,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executavel,
            Arguments = argumentos,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        if (senha is not null)
            psi.Environment["PGPASSWORD"] = senha;

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
            throw new InvalidOperationException($"Não foi possível iniciar o {nomeUtilitario}.");

        var erro = await process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{nomeUtilitario} retornou código {process.ExitCode}: {erro.Trim()}");

        if (!File.Exists(arquivoSaida))
            throw new InvalidOperationException($"{nomeUtilitario} concluiu, mas o arquivo de saída não foi encontrado.");
    }

    /// <summary>
    /// Procura o <c>pg_restore</c> primeiro na MESMA pasta do <c>pg_dump</c> que vai gerar o
    /// arquivo: os dois saem juntos em toda instalação do PostgreSQL, e o <c>pg_restore</c>
    /// precisa ser da mesma versão ou mais nova que o <c>pg_dump</c> — um mais antigo, achado
    /// em outro lugar do PATH, recusaria o <c>.dump</c> por "versão de arquivo não suportada".
    /// </summary>
    public static string? LocalizarPgRestore(string pgDumpPath)
    {
        var pasta = Path.GetDirectoryName(pgDumpPath);
        if (!string.IsNullOrEmpty(pasta))
        {
            var vizinho = Path.Combine(pasta, "pg_restore" + Path.GetExtension(pgDumpPath));
            if (File.Exists(vizinho))
                return vizinho;
        }

        return LocalizarNoPath("pg_restore");
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
