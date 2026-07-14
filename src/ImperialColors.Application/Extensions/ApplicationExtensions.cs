using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImperialColors.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IProdutoService, ProdutoService>();
        services.AddSingleton<ICategoriaService, CategoriaService>();
        services.AddSingleton<IMarcaService, MarcaService>();
        services.AddSingleton<IVendaService, VendaService>();
        services.AddSingleton<IClienteService, ClienteService>();
        services.AddSingleton<IFornecedorService, FornecedorService>();
        services.AddSingleton<IListaCompraService, ListaCompraService>();
        services.AddSingleton<ITrocaService, TrocaService>();
        services.AddSingleton<IVendaExternaService, VendaExternaService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IRelatorioAnalyticsService, RelatorioAnalyticsService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUsuarioService, UsuarioService>();
        return services;
    }
}
