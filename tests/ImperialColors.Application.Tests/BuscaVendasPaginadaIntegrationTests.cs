using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// A busca da tela de Vendas foi reescrita de um <c>WHERE</c> com OR atravessando o JOIN de
/// clientes para um UNION de três ramos indexáveis (152 ms → 0,63 ms com 200 mil vendas).
/// Reescrita de consulta é onde regressão silenciosa mora: uma busca rápida que devolve o
/// conjunto errado é pior que uma lenta. Estes testes fixam o comportamento observável —
/// quais vendas casam, quais não, e que uma venda que casa em vários ramos aparece UMA vez.
///
/// Tudo dentro de uma transação revertida; não deixa rastro no banco de desenvolvimento.
/// </summary>
public class BuscaVendasPaginadaIntegrationTests
{
    private const string Termo = "ZZBUSCA";

    private sealed record Cenario(
        AppDbContext Contexto,
        int SoNumero,
        int SoCliente,
        int SoCupom,
        int TodosOsRamos,
        int ClienteInativo,
        int SemCorrespondencia,
        int Aberta);

    /// <summary>Monta as sete vendas que cobrem cada caminho da busca.</summary>
    private static async Task<Cenario> MontarCenarioAsync(AppDbContext ctx)
    {
        var clienteAtivo = new Cliente { Nome = $"Joao {Termo} Silva" };
        var clienteInativo = new Cliente { Nome = $"Maria {Termo} Removida", Ativo = false };
        ctx.Clientes.AddRange(clienteAtivo, clienteInativo);
        await ctx.SaveChangesAsync();

        Venda Nova(string numero, Cliente? cliente, string? cupom, StatusVenda status = StatusVenda.Finalizada) => new()
        {
            NumeroVenda = numero,
            Status = status,
            Subtotal = 10m,
            Total = 10m,
            DataVenda = DateTime.Now,
            ClienteId = cliente?.Id,
            NomeCompradorCupom = cupom
        };

        var soNumero = Nova($"{Termo}-001", null, null);
        var soCliente = Nova($"V-{Guid.NewGuid():N}"[..12], clienteAtivo, null);
        var soCupom = Nova($"V-{Guid.NewGuid():N}"[..12], null, $"Pedro {Termo} Cupom");
        var todosOsRamos = Nova($"{Termo}-002", clienteAtivo, $"{Termo} tambem");
        var comClienteInativo = Nova($"V-{Guid.NewGuid():N}"[..12], clienteInativo, null);
        var semCorrespondencia = Nova($"V-{Guid.NewGuid():N}"[..12], null, null);
        var aberta = Nova($"{Termo}-003", null, null, StatusVenda.Aberta);

        ctx.Set<Venda>().AddRange(soNumero, soCliente, soCupom, todosOsRamos,
            comClienteInativo, semCorrespondencia, aberta);
        await ctx.SaveChangesAsync();

        return new Cenario(ctx, soNumero.Id, soCliente.Id, soCupom.Id, todosOsRamos.Id,
            comClienteInativo.Id, semCorrespondencia.Id, aberta.Id);
    }

    [Fact]
    public async Task Busca_CobreOsTresRamosSemDuplicarNemVazar()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        var c = await MontarCenarioAsync(ctx);

        // O repositório abre o próprio contexto pelo factory, então não enxergaria uma
        // transação aberta aqui. Para o teste ficar isolado, exercita a MESMA consulta
        // através de um factory que devolve este contexto.
        var repositorio = new VendaRepository(new FactoryDeContextoFixo(ctx));

        var (itens, total) = await repositorio.ObterPaginadoPorPeriodoAsync(
            DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1),
            pagina: 1, itensPorPagina: 50, termoBusca: Termo);

        var ids = itens.Select(v => v.Id).ToList();

        Assert.Contains(c.SoNumero, ids);
        Assert.Contains(c.SoCliente, ids);
        Assert.Contains(c.SoCupom, ids);
        Assert.Contains(c.TodosOsRamos, ids);

        // Cliente soft-deletado não deve puxar a venda: o filtro global de Cliente vale tanto
        // no Include antigo quanto na subconsulta nova.
        Assert.DoesNotContain(c.ClienteInativo, ids);
        Assert.DoesNotContain(c.SemCorrespondencia, ids);
        // Venda Aberta (carrinho em andamento) nunca aparece nesta listagem.
        Assert.DoesNotContain(c.Aberta, ids);

        // Casou nos três ramos, mas o UNION deduplica — tem que aparecer uma vez só.
        Assert.Equal(1, ids.Count(id => id == c.TodosOsRamos));

        Assert.Equal(4, total);
        Assert.Equal(total, ids.Count);

        // O Include(Cliente) precisa sobreviver ao fetch em duas etapas (Ids e depois entidades).
        var comCliente = itens.Single(v => v.Id == c.SoCliente);
        Assert.NotNull(comCliente.Cliente);
        Assert.Contains(Termo, comCliente.Cliente!.Nome);

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task Busca_OrdenaPorDataDecrescenteEPagina()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        await MontarCenarioAsync(ctx);
        var repositorio = new VendaRepository(new FactoryDeContextoFixo(ctx));

        var (primeira, total) = await repositorio.ObterPaginadoPorPeriodoAsync(
            DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1),
            pagina: 1, itensPorPagina: 2, termoBusca: Termo);

        var (segunda, _) = await repositorio.ObterPaginadoPorPeriodoAsync(
            DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1),
            pagina: 2, itensPorPagina: 2, termoBusca: Termo);

        Assert.Equal(4, total);
        Assert.Equal(2, primeira.Count);
        Assert.Equal(2, segunda.Count);

        // Páginas disjuntas e ordem decrescente preservada entre elas.
        Assert.Empty(primeira.Select(v => v.Id).Intersect(segunda.Select(v => v.Id)));
        Assert.True(primeira.Min(v => v.DataVenda) >= segunda.Max(v => v.DataVenda));

        await tx.RollbackAsync();
    }

    /// <summary>Devolve sempre o mesmo contexto (o da transação do teste), ignorando o
    /// descarte — o repositório faz <c>await using</c> no que recebe.</summary>
    private sealed class FactoryDeContextoFixo(AppDbContext contexto) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new ContextoNaoDescartavel(contexto);

        private sealed class ContextoNaoDescartavel(AppDbContext interno)
            : AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(interno.Database.GetDbConnection())
                .Options)
        {
            public override void Dispose() { }
            public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
