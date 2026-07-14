using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

public class AuthServiceIntegrationTests
{
    [Fact]
    public async Task LoginAsync_NoBancoReal_AdminPadraoFunciona()
    {
        var envPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"));
        if (!File.Exists(envPath))
            return;

        DotNetEnv.Env.Load(envPath);

        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
            return;

        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "imperial_colors";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var ssl = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Prefer";
        var adminUser = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
        var adminPass = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@1234";

        var cs = $"Host={host};Port={port};Database={db};Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true;";

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var sessao = await auth.LoginAsync(new LoginDto
        {
            Username = adminUser,
            Senha = adminPass
        });

        Assert.Equal(adminUser.ToLowerInvariant(), sessao.Username);
    }
}
