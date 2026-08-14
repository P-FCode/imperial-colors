using ImperialColors.Application.Configuration;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Configuration;
using ImperialColors.Infrastructure.Contingency;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Fiscal;
using ImperialColors.Infrastructure.Repositories;
using ImperialColors.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImperialColors.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Pooled: o padrão do projeto é abrir um DbContext por método de repositório, então
        // uma única tela dispara dezenas de instanciações. O pool reaproveita os objetos
        // internos (model, change tracker, conexão), tirando esse custo do caminho quente do
        // PDV. Sem a versão pooled, cada CreateDbContext reconstruía tudo do zero.
        //
        // CommandTimeout explícito: deixa o limite declarado no código em vez de depender do
        // padrão do provider, para uma consulta pesada não travar a interface indefinidamente.
        //
        // ⚠️ EnableRetryOnFailure está DELIBERADAMENTE DESLIGADO. Ligá-lo compila, passa em
        // todos os testes e QUEBRA EM PRODUÇÃO: com uma estratégia de retry ativa, o EF lança
        // InvalidOperationException ("does not support user-initiated transactions") assim que
        // um SaveChanges/ExecuteUpdate roda dentro de um BeginTransaction manual — e existem 11
        // desses, incluindo CriarComBaixaEstoqueTransacionalAsync (toda venda),
        // AjustarEstoqueAsync e as trocas. Curiosamente o BeginTransaction sozinho não lança,
        // então um teste superficial passa; o erro só aparece na primeira venda real.
        //
        // Para ligar o retry é preciso, antes, envolver cada um desses 11 blocos em
        // Database.CreateExecutionStrategy().ExecuteAsync(...) — e mover a CRIAÇÃO do
        // DbContext para dentro do lambda, senão a segunda tentativa reusa um contexto com as
        // entidades já rastreadas da tentativa anterior. É refatoração no código mais crítico
        // do sistema (venda + baixa de estoque) e merece PR própria, não um efeito colateral
        // de um ajuste de performance.
        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.CommandTimeout(30)));

        var caminhoSqlite = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ImperialColors",
            "pdv_contingency.db");
        Directory.CreateDirectory(Path.GetDirectoryName(caminhoSqlite)!);

        services.AddDbContextFactory<ContingencyDbContext>(options =>
            options.UseSqlite($"Data Source={caminhoSqlite}"));

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
        services.AddSingleton<ILogAuditoriaRepository, LogAuditoriaRepository>();
        services.AddSingleton<ITributacaoProdutoRepository, TributacaoProdutoRepository>();
        services.AddSingleton<ITributacaoCategoriaRepository, TributacaoCategoriaRepository>();
        services.AddSingleton<IConfiguracaoFiscalEmpresaRepository, ConfiguracaoFiscalEmpresaRepository>();
        services.AddSingleton<IRepository<NaturezaOperacao>, RepositoryBase<NaturezaOperacao>>();
        services.AddSingleton<INotaFiscalRepository, NotaFiscalRepository>();

        services.AddSingleton<IContingencyVendaService, ContingencyVendaService>();

        services.AddSingleton<DatabaseHealthService>();
        services.AddSingleton<IDatabaseHealthService>(sp => sp.GetRequiredService<DatabaseHealthService>());
        services.AddHostedService(sp => sp.GetRequiredService<DatabaseHealthService>());

        services.AddSingleton<DataSyncService>();
        services.AddSingleton<IDataSyncService>(sp => sp.GetRequiredService<DataSyncService>());
        services.AddHostedService(sp => sp.GetRequiredService<DataSyncService>());

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

        services.AddHttpClient<INcmService, BrasilApiNcmService>(client =>
        {
            client.BaseAddress = new Uri("https://brasilapi.com.br/");
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ImperialColors/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        // API Fiscal (PFCode) — dois HttpClients nomeados (NF-e :5001 / NFC-e :5002),
        // ver comentário em FiscalApiClient sobre por que não é o AddHttpClient<T> típico.
        services.AddHttpClient(FiscalApiClient.ClienteNFe, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<FiscalApiConfig>>().Value;
            client.BaseAddress = new Uri(config.NFeBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient(FiscalApiClient.ClienteNFCe, (sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<FiscalApiConfig>>().Value;
            client.BaseAddress = new Uri(config.NFCeBaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddTransient<IFiscalApiClient, FiscalApiClient>();

        return services;
    }
}
