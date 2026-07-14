using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Critérios obrigatórios:
/// 1) Produto GL 18L e BD persistem corretamente no banco.
/// 2) Troca de item R$100 por R$150 calcula diferença R$50 e atualiza estoques.
/// </summary>
public class TrocaIntegrationTests
{
    private static bool TryConfigurar(out ServiceProvider provider)
    {
        provider = null!;
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();
        provider = services.BuildServiceProvider();
        return true;
    }

    [Fact]
    public async Task CadastrarProdutoGl18LeBaldeBD_DeveSalvarLitragemEUnidadeCorretas()
    {
        if (!TryConfigurar(out var provider)) return;
        await using var scope = provider.CreateAsyncScope();

        var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();
        var categoriaRepo = scope.ServiceProvider.GetRequiredService<IRepository<Domain.Entities.Categoria>>();
        var marcaRepo = scope.ServiceProvider.GetRequiredService<IRepository<Domain.Entities.Marca>>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        var sufixo = Guid.NewGuid().ToString("N")[..8];

        var categoria = await categoriaRepo.AdicionarAsync(new Domain.Entities.Categoria { Nome = $"Cat-{sufixo}", Ativo = true });
        var marca = await marcaRepo.AdicionarAsync(new Domain.Entities.Marca { Nome = $"Marca-{sufixo}", Ativo = true });

        // --- Teste 1: Galão 18L ---
        var galao18L = await produtoService.CriarAsync(new CriarProdutoDto
        {
            Nome = $"Tinta Coral GL18L {sufixo}",
            CodigoInterno = $"GL18-{sufixo}",
            CodigoInternoDefinidoManualmente = true,
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            Unidade = "GL",
            LitragemGl = 18m,
            PrecoVenda = 150m,
            Custo = 90m,
            QuantidadeEstoque = 10,
            EstoqueMinimo = 1
        });

        Assert.Equal("GL", galao18L.Unidade);
        Assert.Equal(18m, galao18L.LitragemGl);
        Assert.Contains("18L", galao18L.NomeExibicao);

        // Verifica no banco diretamente
        await using var ctx = await contextFactory.CreateDbContextAsync();
        var prodBanco = await ctx.Set<Domain.Entities.Produto>()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == galao18L.Id);

        Assert.Equal(18m, prodBanco.LitragemGl);

        // --- Teste 2: Balde BD ---
        var balde = await produtoService.CriarAsync(new CriarProdutoDto
        {
            Nome = $"Tinta Balde BD {sufixo}",
            CodigoInterno = $"BD-{sufixo}",
            CodigoInternoDefinidoManualmente = true,
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            Unidade = "BD",
            LitragemGl = null,
            PrecoVenda = 80m,
            Custo = 50m,
            QuantidadeEstoque = 5,
            EstoqueMinimo = 1
        });

        Assert.Equal("BD", balde.Unidade);
        Assert.Null(balde.LitragemGl);

        var baldeBanco = await ctx.Set<Domain.Entities.Produto>()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == balde.Id);
        Assert.Equal("BD", baldeBanco.Unidade);
    }

    [Fact]
    public async Task Troca_ItemR100PorR150_DeveCalcularDiferenca50EAtualizarEstoques()
    {
        if (!TryConfigurar(out var provider)) return;
        await using var scope = provider.CreateAsyncScope();

        var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();
        var vendaService = scope.ServiceProvider.GetRequiredService<IVendaService>();
        var trocaService = scope.ServiceProvider.GetRequiredService<ITrocaService>();
        var categoriaRepo = scope.ServiceProvider.GetRequiredService<IRepository<Domain.Entities.Categoria>>();
        var marcaRepo = scope.ServiceProvider.GetRequiredService<IRepository<Domain.Entities.Marca>>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var categoria = await categoriaRepo.AdicionarAsync(new Domain.Entities.Categoria { Nome = $"Cat-{sufixo}", Ativo = true });
        var marca = await marcaRepo.AdicionarAsync(new Domain.Entities.Marca { Nome = $"Marca-{sufixo}", Ativo = true });

        // Produto devolvido: R$100 com 5 em estoque
        var prodDevolvido = await produtoService.CriarAsync(new CriarProdutoDto
        {
            Nome = $"Tinta A {sufixo}",
            CodigoInterno = $"PA-{sufixo}",
            CodigoInternoDefinidoManualmente = true,
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            Unidade = "GL",
            LitragemGl = 18m,
            PrecoVenda = 100m,
            Custo = 60m,
            QuantidadeEstoque = 5,
            EstoqueMinimo = 1
        });

        // Produto novo: R$150 com 10 em estoque
        var prodNovo = await produtoService.CriarAsync(new CriarProdutoDto
        {
            Nome = $"Tinta B {sufixo}",
            CodigoInterno = $"PB-{sufixo}",
            CodigoInternoDefinidoManualmente = true,
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            Unidade = "GL",
            LitragemGl = 18m,
            PrecoVenda = 150m,
            Custo = 90m,
            QuantidadeEstoque = 10,
            EstoqueMinimo = 1
        });

        // Cria e finaliza uma venda do produto A por R$100
        var venda = await vendaService.CriarAsync(new CriarVendaDto
        {
            FormaPagamento = FormaPagamento.Dinheiro,
            ValorPago = 100m,
            Troco = 0m,
            Itens = new List<CriarItemVendaDto>
            {
                new() { ProdutoId = prodDevolvido.Id, Quantidade = 1, PrecoUnitario = 100m, Desconto = 0 }
            }
        });
        var vendaFinalizada = await vendaService.FinalizarAsync(venda.Id);
        Assert.Equal(StatusVenda.Finalizada, vendaFinalizada.Status);

        // Carrega venda com itens para obter o ItemVendaId
        var vendaComItens = await vendaService.ObterComItensAsync(venda.Id);
        Assert.NotNull(vendaComItens);
        var itemOrigem = vendaComItens!.Itens.First();

        // Registra troca: devolve produto A (R$100), leva produto B (R$150)
        var trocaDto = await trocaService.RegistrarAsync(new RegistrarTrocaDto
        {
            VendaOrigemId = venda.Id,
            ItemVendaOrigemId = itemOrigem.Id,
            QuantidadeDevolvida = 1,
            RetornarAoEstoque = true,
            ProdutoNovoId = prodNovo.Id,
            QuantidadeNova = 1,
            PrecoUnitarioNovo = 150m,
            FormaPagamentoDiferenca = FormaPagamento.Pix,
            Usuario = "TestRunner"
        });

        // --- Assertions financeiras ---
        Assert.Equal(100m, trocaDto.ValorTotalDevolvido);
        Assert.Equal(150m, trocaDto.ValorTotalNovo);
        Assert.Equal(50m, trocaDto.DiferencaValor);  // diferença a receber = R$50
        Assert.Equal(FormaPagamento.Pix, trocaDto.FormaPagamentoDiferenca);

        // --- Assertions de estoque ---
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var estoqueDevolvidoAtual = (await ctx.Set<Domain.Entities.Produto>()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == prodDevolvido.Id)).QuantidadeEstoque;

        var estoqueNovoAtual = (await ctx.Set<Domain.Entities.Produto>()
            .IgnoreQueryFilters()
            .FirstAsync(p => p.Id == prodNovo.Id)).QuantidadeEstoque;

        // RetornarAoEstoque=true: estoque A volta para 5 (5 - 1 saída venda + 1 retorno troca = 5)
        Assert.Equal(5m, estoqueDevolvidoAtual);

        // Produto B saiu 1 da troca: 10 - 1 = 9
        Assert.Equal(9m, estoqueNovoAtual);
    }
}
