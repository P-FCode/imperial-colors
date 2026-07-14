using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class RelatorioAnalyticsRepository : IRelatorioAnalyticsRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public RelatorioAnalyticsRepository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<LinhaRelatorioVendaExternaResumo>> ObterLinhasVendasExternasPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        return await context.ItensVendaExterna
            .AsNoTracking()
            .Where(i => i.VendaExterna.DataVenda >= inicio && i.VendaExterna.DataVenda <= fim)
            .OrderBy(i => i.VendaExterna.DataVenda)
            .ThenBy(i => i.VendaExterna.NumeroVendaExterna)
            .ThenBy(i => i.Id)
            .Select(i => new LinhaRelatorioVendaExternaResumo
            {
                DataVenda = i.VendaExterna.DataVenda,
                CodigoVenda = i.VendaExterna.NumeroVendaExterna,
                ProdutoItem = i.NomeProduto,
                QuantidadeVendida = i.Quantidade,
                ValorUnitario = i.PrecoUnitario,
                ValorTotal = i.Subtotal
            })
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ProdutoRankingResumo>> ObterProdutosMaisVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
        => ObterRankingAgregadoAsync(inicio, fim, decrescente: true, cancellationToken);

    public Task<IReadOnlyList<ProdutoRankingResumo>> ObterProdutosMenosVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
        => ObterRankingAgregadoAsync(inicio, fim, decrescente: false, cancellationToken);

    public async Task<IReadOnlyList<ProdutoEncalhadoResumo>> ObterProdutosNuncaVendidosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        return await context.Produtos
            .AsNoTracking()
            .Where(p => p.Ativo && p.QuantidadeEstoque > 0)
            .Where(p => !context.ItensVenda.Any(i =>
                i.ProdutoId == p.Id &&
                i.Venda.Status == StatusVenda.Finalizada &&
                i.Venda.DataVenda >= inicio &&
                i.Venda.DataVenda <= fim))
            .Where(p => !context.ItensVendaExterna.Any(i =>
                i.ProdutoId == p.Id &&
                i.VendaExterna.DataVenda >= inicio &&
                i.VendaExterna.DataVenda <= fim))
            .OrderBy(p => p.Nome)
            .Select(p => new ProdutoEncalhadoResumo
            {
                CodigoInterno = p.CodigoInterno,
                NomeProduto = p.Nome,
                EstoqueAtual = p.QuantidadeEstoque,
                ValorTotalParado = p.QuantidadeEstoque * (p.Custo ?? 0m)
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ProdutoRankingResumo>> ObterRankingAgregadoAsync(
        DateTime inicio, DateTime fim, bool decrescente, CancellationToken cancellationToken)
    {
        await using var context = _contextFactory.CreateDbContext();

        var balcao = await context.ItensVenda
            .AsNoTracking()
            .Where(i => i.Venda.Status == StatusVenda.Finalizada &&
                        i.Venda.DataVenda >= inicio &&
                        i.Venda.DataVenda <= fim)
            .GroupBy(i => i.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Quantidade = g.Sum(x => x.Quantidade),
                Faturamento = g.Sum(x => x.Subtotal)
            })
            .ToListAsync(cancellationToken);

        var externas = await context.ItensVendaExterna
            .AsNoTracking()
            .Where(i => i.ProdutoId.HasValue &&
                        i.VendaExterna.DataVenda >= inicio &&
                        i.VendaExterna.DataVenda <= fim)
            .GroupBy(i => i.ProdutoId!.Value)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Quantidade = g.Sum(x => x.Quantidade),
                Faturamento = g.Sum(x => x.Subtotal)
            })
            .ToListAsync(cancellationToken);

        if (balcao.Count == 0 && externas.Count == 0)
            return Array.Empty<ProdutoRankingResumo>();

        var agregado = balcao
            .Concat(externas)
            .GroupBy(x => x.ProdutoId)
            .Select(g => new
            {
                ProdutoId = g.Key,
                Quantidade = g.Sum(x => x.Quantidade),
                Faturamento = g.Sum(x => x.Faturamento)
            })
            .Where(x => x.Quantidade > 0)
            .ToList();

        if (agregado.Count == 0)
            return Array.Empty<ProdutoRankingResumo>();

        var ids = agregado.Select(a => a.ProdutoId).ToList();
        var produtos = await context.Produtos
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.CodigoInterno, p.Nome })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var ordenado = decrescente
            ? agregado.OrderByDescending(a => a.Quantidade).ThenByDescending(a => a.Faturamento)
            : agregado.OrderBy(a => a.Quantidade).ThenBy(a => a.Faturamento);

        return ordenado
            .Where(a => produtos.ContainsKey(a.ProdutoId))
            .Select(a => new ProdutoRankingResumo
            {
                CodigoInterno = produtos[a.ProdutoId].CodigoInterno,
                NomeProduto = produtos[a.ProdutoId].Nome,
                QuantidadeTotal = a.Quantidade,
                FaturamentoGerado = a.Faturamento
            })
            .ToList();
    }
}
