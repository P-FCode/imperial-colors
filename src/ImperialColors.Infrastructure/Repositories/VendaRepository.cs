using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class VendaRepository : RepositoryBase<Venda>, IVendaRepository
{
    public VendaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<Venda?> ObterComItensAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Pagamentos)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Venda>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Finalizada)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }

    public async Task<decimal> ObterTotalVendasDiaAsync(DateTime data)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada && v.DataVenda.Date == data.Date)
            .SumAsync(v => v.Total);
    }

    public async Task<decimal> ObterTotalVendasMesAsync(int ano, int mes)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada &&
                        v.DataVenda.Year == ano && v.DataVenda.Month == mes)
            .SumAsync(v => v.Total);
    }

    public async Task<string> GerarNumeroVendaAsync()
    {
        await using var context = ContextFactory.CreateDbContext();
        var hoje = DateTime.Today;
        var prefixo = hoje.ToString("yyyyMMdd");
        var ultimaVenda = await context.Set<Venda>()
            .Where(v => v.NumeroVenda.StartsWith(prefixo))
            .OrderByDescending(v => v.NumeroVenda)
            .FirstOrDefaultAsync();

        var sequencial = 1;
        if (ultimaVenda is not null)
        {
            var partes = ultimaVenda.NumeroVenda.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out var seq))
                sequencial = seq + 1;
        }

        return $"{prefixo}-{sequencial:D4}";
    }

    public async Task<(IReadOnlyList<Venda> Itens, int Total)> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status != StatusVenda.Aberta);

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(v =>
                EF.Functions.ILike(v.NumeroVenda, $"%{termo}%") ||
                (v.Cliente != null && EF.Functions.ILike(v.Cliente.Nome, $"%{termo}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderByDescending(v => v.DataVenda)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<IReadOnlyList<Venda>> ObterUltimasFinalizadasAsync(
        int quantidade = 5,
        CancellationToken cancellationToken = default)
    {
        quantidade = Math.Clamp(quantidade, 1, 50);

        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada)
            .OrderByDescending(v => v.DataVenda)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public async Task CancelarComEstornoAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var venda = await context.Set<Venda>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

            if (venda.Status == StatusVenda.Cancelada)
                throw new DomainException("Venda já está cancelada.");

            if (venda.Status == StatusVenda.Finalizada)
            {
                foreach (var item in venda.Itens)
                {
                    var produto = await context.Set<Produto>()
                        .FirstOrDefaultAsync(p => p.Id == item.ProdutoId, cancellationToken)
                        ?? throw new DomainException($"Produto com Id {item.ProdutoId} não encontrado para estorno.");

                    var quantidadeAnterior = produto.QuantidadeEstoque;
                    produto.QuantidadeEstoque += item.Quantidade;

                    context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacao.Entrada,
                        Quantidade = item.Quantidade,
                        QuantidadeAnterior = quantidadeAnterior,
                        QuantidadeAtual = produto.QuantidadeEstoque,
                        Motivo = $"Cancelamento venda #{venda.NumeroVenda}",
                        VendaId = vendaId
                    });
                }
            }

            venda.Status = StatusVenda.Cancelada;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExcluirFisicamenteComEstornoAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var possuiTrocas = await context.Set<Troca>()
                .IgnoreQueryFilters()
                .AnyAsync(t => t.VendaOrigemId == vendaId, cancellationToken);

            if (possuiTrocas)
                throw new DomainException("Não é possível excluir esta venda porque existem trocas registradas vinculadas a ela.");

            var venda = await context.Set<Venda>()
                .IgnoreQueryFilters()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

            if (venda.Status == StatusVenda.Finalizada)
            {
                foreach (var item in venda.Itens)
                {
                    var produto = await context.Set<Produto>()
                        .FirstOrDefaultAsync(p => p.Id == item.ProdutoId, cancellationToken)
                        ?? throw new DomainException($"Produto com Id {item.ProdutoId} não encontrado para estorno.");

                    var quantidadeAnterior = produto.QuantidadeEstoque;
                    produto.QuantidadeEstoque += item.Quantidade;

                    context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacao.Entrada,
                        Quantidade = item.Quantidade,
                        QuantidadeAnterior = quantidadeAnterior,
                        QuantidadeAtual = produto.QuantidadeEstoque,
                        Motivo = $"Exclusão permanente venda #{venda.NumeroVenda}",
                        VendaId = vendaId
                    });
                }
            }

            context.Set<Venda>().Remove(venda);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<IEnumerable<Venda>> ObterTodosAsync()
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }
}
