using DotNetEnv;
using ImperialColors.Application.Configuration;
using ImperialColors.Application.Extensions;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Extensions;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.ViewModels;
using ImperialColors.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Windows;

namespace ImperialColors.UI;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        ConfigurarCulturaPtBr();
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Erro inesperado:\n\n{args.Exception.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    $"Erro crítico:\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            MessageBox.Show(
                $"Erro em operação assíncrona:\n\n{args.Exception.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.SetObserved();
        };

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        CarregarArquivoEnv();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<EmpresaConfig>(ctx.Configuration.GetSection(EmpresaConfig.Secao));
                services.PostConfigure<EmpresaConfig>(EmpresaConfigEnvironmentOverrides.Aplicar);

                var connectionString = AppConfigService.MontarConnectionString();
                services.AddInfrastructure(connectionString);
                services.AddApplication();

                services.AddSingleton<IAppConfigService, AppConfigService>();
                services.AddSingleton<ISessaoService, SessaoService>();
                services.AddSingleton<IRelatorioService, RelatorioService>();
                services.AddSingleton<DocumentosPdfService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<GestaoUsuariosViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ProdutoViewModel>();
                services.AddTransient<VendaViewModel>();
                services.AddTransient<ClienteViewModel>();
                services.AddTransient<FornecedorViewModel>();
                services.AddTransient<ListaCompraViewModel>();
                services.AddTransient<VendaExternaViewModel>();

                services.AddTransient<LoginView>();
                services.AddTransient<ImperialColors.UI.Views.MainWindow>();
                services.AddTransient<PDVView>();
                services.AddTransient<ProdutoFormView>();
                services.AddTransient<MovimentacaoEstoqueView>();
                services.AddTransient<ClienteFormView>();
                services.AddTransient<FornecedorFormView>();
                services.AddTransient<ListaCompraFormView>();
                services.AddTransient<TrocaFormView>();
                services.AddTransient<VendaExternaFormView>();
                services.AddTransient<CupomView>();
                services.AddTransient<GestaoUsuariosView>();
                services.AddTransient<PerifericosView>();

                services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        await _host.StartAsync();

        try
        {
            using var scope = _host.Services.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<App>>();
            await dbContext.Database.MigrateAsync();
            await UsuarioDatabaseSeeder.SeedAdminAsync(dbContext, logger);
        }
        catch (Exception ex)
        {
            var resultado = MessageBox.Show(
                $"Erro ao conectar com o banco de dados:\n\n{ex.Message}\n\n" +
                "Verifique o arquivo .env e tente novamente.\n\nDeseja continuar mesmo assim?",
                "Erro de Banco de Dados",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (resultado == MessageBoxResult.No)
            {
                Shutdown();
                return;
            }
        }

        try
        {
            var loginView = _host.Services.GetRequiredService<LoginView>();
            if (loginView.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var mainWindow = _host.Services.GetRequiredService<ImperialColors.UI.Views.MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Closed += (_, _) => Shutdown();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível iniciar o sistema após o login:\n\n{ex.Message}",
                "Erro ao Iniciar",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(System.Windows.ExitEventArgs e)
    {
        _host?.Services.GetService<ISessaoService>()?.EncerrarSessao();

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    internal static void ConfigurarCulturaPtBr()
        => FormattingHelper.ConfigurarCulturaAplicacao();

    private static void CarregarArquivoEnv()
    {
        var caminhos = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".env")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", ".env"))
        };

        foreach (var caminho in caminhos.Distinct())
        {
            if (File.Exists(caminho))
            {
                Env.Load(caminho);
                return;
            }
        }
    }
}
