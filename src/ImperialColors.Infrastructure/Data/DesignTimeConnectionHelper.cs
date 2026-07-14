namespace ImperialColors.Infrastructure.Data;

internal static class DesignTimeConnectionHelper
{
    public static string ObterConnectionString()
    {
        CarregarArquivoEnv();
        return MontarConnectionString();
    }

    private static void CarregarArquivoEnv()
    {
        var candidatos = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".env")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"))
        };

        foreach (var caminho in candidatos.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(caminho))
                continue;

            foreach (var linha in File.ReadAllLines(caminho))
            {
                if (string.IsNullOrWhiteSpace(linha) || linha.TrimStart().StartsWith('#'))
                    continue;

                var indice = linha.IndexOf('=');
                if (indice <= 0)
                    continue;

                var chave = linha[..indice].Trim();
                var valor = linha[(indice + 1)..].Trim();
                if (chave.Length == 0)
                    continue;

                Environment.SetEnvironmentVariable(chave, valor);
            }

            return;
        }
    }

    private static string MontarConnectionString()
    {
        var host = Obter("DB_HOST", "localhost");
        var port = Obter("DB_PORT", "5432");
        var database = Obter("DB_NAME", "imperial_colors");
        var username = Obter("DB_USER", "postgres");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        var sslMode = Obter("DB_SSL_MODE", "Prefer");

        return
            $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true;";
    }

    private static string Obter(string chave, string padrao)
        => Environment.GetEnvironmentVariable(chave)?.Trim() is { Length: > 0 } valor ? valor : padrao;
}
