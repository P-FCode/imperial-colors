namespace ImperialColors.Application.Tests;

/// <summary>
/// Evita que testes de integração gravem dados no banco de desenvolvimento
/// quando RUN_INTEGRATION_TESTS não estiver definido como true.
/// </summary>
internal static class IntegrationTestGuard
{
    public static bool TryObterConnectionString(out string connectionString)
    {
        connectionString = string.Empty;

        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase))
            return false;

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var envPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"));
        if (!File.Exists(envPath))
            return false;

        DotNetEnv.Env.Load(envPath);

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
            return false;

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "imperial_colors";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var ssl = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Prefer";

        connectionString =
            $"Host={host};Port={port};Database={dbName};Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true;";
        return true;
    }
}
