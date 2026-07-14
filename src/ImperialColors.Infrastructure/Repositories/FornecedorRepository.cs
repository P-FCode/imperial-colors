using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class FornecedorRepository : RepositoryBase<Fornecedor>, IFornecedorRepository
{
    public FornecedorRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<IEnumerable<Fornecedor>> BuscarPorNomeAsync(string nome)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Fornecedor>()
            .AsNoTracking()
            .Where(f => EF.Functions.ILike(f.Nome, $"%{nome}%"))
            .OrderBy(f => f.Nome)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Fornecedor> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<Fornecedor>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(f => EF.Functions.ILike(f.Nome, $"%{termo}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderBy(f => f.Nome)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<bool> PossuiVinculosAsync(int fornecedorId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();

        if (await context.Set<Produto>()
                .IgnoreQueryFilters()
                .AnyAsync(p => p.FornecedorId == fornecedorId, cancellationToken))
            return true;

        return await context.Set<ListaCompra>()
            .IgnoreQueryFilters()
            .AnyAsync(l => l.FornecedorId == fornecedorId, cancellationToken);
    }

    public async Task RemoverFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var fornecedor = await context.Set<Fornecedor>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (fornecedor is null)
            return;

        context.Set<Fornecedor>().Remove(fornecedor);
        await SalvarAlteracoesAsync(context);
    }

    public async Task<bool> ExisteFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Fornecedor>()
            .IgnoreQueryFilters()
            .AnyAsync(f => f.Id == id, cancellationToken);
    }

    public override async Task RemoverAsync(int id)
    {
        await RemoverFisicamenteAsync(id);
    }
}
