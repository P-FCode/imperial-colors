using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class MovimentacaoEstoqueRepository : RepositoryBase<MovimentacaoEstoque>, IMovimentacaoEstoqueRepository
{
    public MovimentacaoEstoqueRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<IEnumerable<MovimentacaoEstoque>> ObterPorProdutoAsync(int produtoId)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<MovimentacaoEstoque>()
            .AsNoTracking()
            .Include(m => m.Produto)
            .Where(m => m.ProdutoId == produtoId)
            .OrderByDescending(m => m.CriadoEm)
            .ToListAsync();
    }

    public async Task<IEnumerable<MovimentacaoEstoque>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<MovimentacaoEstoque>()
            .AsNoTracking()
            .Include(m => m.Produto)
            .Where(m => m.CriadoEm >= inicio && m.CriadoEm <= fim)
            .OrderByDescending(m => m.CriadoEm)
            .ToListAsync();
    }
}
