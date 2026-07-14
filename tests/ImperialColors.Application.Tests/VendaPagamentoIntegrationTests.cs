using DotNetEnv;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

public class VendaPagamentoIntegrationTests
{
    private static bool TryCarregarConfig(out ServiceProvider provider, out IDbContextFactory<AppDbContext> contextFactory)
    {
        provider = null!;
        contextFactory = null!;

        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        provider = services.BuildServiceProvider();
        contextFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var dbContext = contextFactory.CreateDbContext();
        dbContext.Database.Migrate();
        return true;
    }

    [Theory]
    [InlineData(FormaPagamento.Dinheiro, 150, 50, 1)]
    [InlineData(FormaPagamento.CartaoCredito, 100, 0, 4)]
    [InlineData(FormaPagamento.Pix, 100, 0, 1)]
    [InlineData(FormaPagamento.CartaoDebito, 100, 0, 1)]
    [InlineData(FormaPagamento.Boleto, 100, 0, 1)]
    public async Task CriarVenda_ComFormasPagamento_PersisteNoBanco(
        FormaPagamento forma, decimal valorPago, decimal troco, int parcelas)
    {
        if (!TryCarregarConfig(out var provider, out var contextFactory))
            return;

        await using var scope = provider.CreateAsyncScope();
        var vendaService = scope.ServiceProvider.GetRequiredService<IVendaService>();
        var produtoRepository = scope.ServiceProvider.GetRequiredService<IProdutoRepository>();

        var produto = (await produtoRepository.ObterTodosAsync()).FirstOrDefault();
        if (produto is null)
            return;

        if (produto.QuantidadeEstoque < 1)
        {
            produto.QuantidadeEstoque = 10;
            await produtoRepository.AtualizarAsync(produto);
        }

        var total = produto.PrecoVenda;

        if (forma == FormaPagamento.Dinheiro)
            valorPago = total + troco;

        var venda = await vendaService.CriarAsync(new CriarVendaDto
        {
            Usuario = "teste_integracao",
            FormaPagamento = forma,
            QuantidadeParcelas = parcelas,
            ValorPago = valorPago,
            Troco = troco,
            Itens =
            [
                new CriarItemVendaDto
                {
                    ProdutoId = produto.Id,
                    Quantidade = 1,
                    PrecoUnitario = produto.PrecoVenda
                }
            ]
        });

        try
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            var noBanco = await db.Vendas.AsNoTracking()
                .Include(v => v.Pagamentos)
                .FirstAsync(v => v.Id == venda.Id);

            Assert.Equal(venda.FormaPagamento, noBanco.FormaPagamento);
            Assert.Equal(venda.QuantidadeParcelas, noBanco.QuantidadeParcelas);
            Assert.Equal(venda.ValorPago, noBanco.ValorPago);
            Assert.Equal(venda.Troco, noBanco.Troco);

            if (venda.Pagamentos.Count > 0)
            {
                Assert.Equal(venda.Pagamentos.Count, noBanco.Pagamentos.Count);
                Assert.Equal(venda.Total, noBanco.Pagamentos.Sum(p => p.Valor));
            }
        }
        finally
        {
            await vendaService.CancelarAsync(venda.Id);
        }
    }
}
