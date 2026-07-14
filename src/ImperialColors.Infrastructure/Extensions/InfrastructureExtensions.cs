using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Configuration;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using ImperialColors.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ImperialColors.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddSingleton(BackupOptions.CarregarDoAmbiente());
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IParametroSistemaRepository, ParametroSistemaRepository>();

        services.AddSingleton<IProdutoRepository, ProdutoRepository>();
        services.AddSingleton<IVendaRepository, VendaRepository>();
        services.AddSingleton<IClienteRepository, ClienteRepository>();
        services.AddSingleton<IFornecedorRepository, FornecedorRepository>();
        services.AddSingleton<IListaCompraRepository, ListaCompraRepository>();
        services.AddSingleton<IMovimentacaoEstoqueRepository, MovimentacaoEstoqueRepository>();
        services.AddSingleton<IRepository<Categoria>, CategoriaRepository>();
        services.AddSingleton<IRepository<Marca>, MarcaRepository>();
        services.AddSingleton<IRepository<ListaCompra>, ListaCompraRepository>();
        services.AddSingleton<IRepository<ItemListaCompra>, RepositoryBase<ItemListaCompra>>();
        services.AddSingleton<IUsuarioRepository, UsuarioRepository>();
        services.AddSingleton<ITrocaRepository, TrocaRepository>();
        services.AddSingleton<IVendaExternaRepository, VendaExternaRepository>();
        services.AddSingleton<IRelatorioAnalyticsRepository, RelatorioAnalyticsRepository>();

        services.AddSingleton<IPrinterService, PrinterService>();
        services.AddSingleton<ILocalConfigService, LocalConfigService>();

        services.AddHttpClient<IViaCepService, ViaCepService>(client =>
        {
            client.BaseAddress = new Uri("https://viacep.com.br/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ImperialColors/1.0");
        });

        services.AddHttpClient<ReceitaWsCnpjService>(client =>
        {
            client.BaseAddress = new Uri("https://receitaws.com.br/");
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ImperialColors/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddHttpClient<BrasilApiCnpjService>(client =>
        {
            client.BaseAddress = new Uri("https://brasilapi.com.br/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ImperialColors/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.AddTransient<ICnpjConsultaService, CnpjConsultaCompostaService>();

        return services;
    }
}
