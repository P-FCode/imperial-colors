using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ImperialColors.UI.Services;

public interface IArquivoEnvService
{
    /// <summary>Arquivo .env que a instalação usa — o mesmo que foi lido na abertura.</summary>
    string Caminho { get; }

    bool Existe { get; }

    /// <summary>
    /// Grava as chaves informadas no .env e reflete os valores no processo atual, para que a
    /// alteração valha sem reiniciar. Chaves não citadas ficam intactas.
    /// </summary>
    Task SalvarAsync(IReadOnlyDictionary<string, string> valores, CancellationToken cancellationToken = default);
}

/// <summary>
/// Escrita do .env preservando comentários, ordem e chaves que a tela não conhece.
/// A gravação é atômica (arquivo temporário + troca) para que uma queda de energia no meio
/// do salvamento não deixe o cliente com um .env truncado — sem ele o sistema não abre.
/// </summary>
public class ArquivoEnvService : IArquivoEnvService
{
    // Só captura atribuições reais: linha comentada (# CHAVE=valor) não é alterada, senão
    // salvar pela tela "descomentaria" configurações que o instalador deixou desligadas.
    private static readonly Regex PadraoAtribuicao = new(
        @"^(?<indentacao>\s*)(?<chave>[A-Za-z_][A-Za-z0-9_]*)\s*=",
        RegexOptions.Compiled);

    public ArquivoEnvService()
    {
        Caminho = ArquivoEnvLocalizador.Resolver();
    }

    public string Caminho { get; }

    public bool Existe => File.Exists(Caminho);

    public async Task SalvarAsync(
        IReadOnlyDictionary<string, string> valores,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valores);

        if (valores.Count == 0)
            return;

        var linhas = Existe
            ? (await File.ReadAllLinesAsync(Caminho, cancellationToken)).ToList()
            : new List<string>();

        var pendentes = new Dictionary<string, string>(valores, StringComparer.Ordinal);

        for (var i = 0; i < linhas.Count; i++)
        {
            var correspondencia = PadraoAtribuicao.Match(linhas[i]);
            if (!correspondencia.Success)
                continue;

            var chave = correspondencia.Groups["chave"].Value;
            if (!pendentes.Remove(chave, out var valor))
                continue;

            linhas[i] = $"{correspondencia.Groups["indentacao"].Value}{chave}={Escapar(valor)}";
        }

        if (pendentes.Count > 0)
        {
            if (linhas.Count > 0 && !string.IsNullOrWhiteSpace(linhas[^1]))
                linhas.Add(string.Empty);

            linhas.Add("# --- Adicionado pelo sistema (Configurações > Geral) ---");
            linhas.AddRange(pendentes.Select(par => $"{par.Key}={Escapar(par.Value)}"));
        }

        await GravarAtomicamenteAsync(linhas, cancellationToken);

        foreach (var (chave, valor) in valores)
            Environment.SetEnvironmentVariable(chave, valor);
    }

    private async Task GravarAtomicamenteAsync(IReadOnlyList<string> linhas, CancellationToken cancellationToken)
    {
        var diretorio = Path.GetDirectoryName(Caminho);
        if (!string.IsNullOrEmpty(diretorio))
            Directory.CreateDirectory(diretorio);

        // Temporário no mesmo diretório: File.Move entre volumes diferentes não é atômico.
        var temporario = Caminho + ".tmp";
        await File.WriteAllLinesAsync(temporario, linhas, new UTF8Encoding(false), cancellationToken);
        File.Move(temporario, Caminho, overwrite: true);
    }

    /// <summary>
    /// O leitor (DotNetEnv) corta o valor num "#" solto e remove espaços das pontas. Valores
    /// nessas condições — um endereço com "nº 100 # fundos", por exemplo — precisam vir entre
    /// aspas para voltarem inteiros na próxima leitura.
    /// </summary>
    private static string Escapar(string valor)
    {
        var normalizado = valor.Replace("\r", string.Empty).Replace("\n", " ");

        var precisaAspas = normalizado.Contains('#')
            || normalizado.Contains('"')
            || normalizado != normalizado.Trim();

        return precisaAspas
            ? $"\"{normalizado.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""
            : normalizado;
    }
}

/// <summary>
/// Resolve qual .env a instalação usa. Leitura (App) e escrita (Configurações) compartilham
/// esta lista de propósito: se divergirem, a tela passa a salvar num arquivo que nunca é lido.
/// </summary>
public static class ArquivoEnvLocalizador
{
    public static IReadOnlyList<string> Candidatos()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;

        return new[]
        {
            Path.Combine(baseDir, ".env"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", ".env"))
        }.Distinct().ToArray();
    }

    public static string? LocalizarExistente()
        => Candidatos().FirstOrDefault(File.Exists);

    /// <summary>Arquivo existente ou, se nenhum existir, onde ele deve ser criado.</summary>
    public static string Resolver()
        => LocalizarExistente() ?? Candidatos()[0];
}
