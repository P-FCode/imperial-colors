using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class ClienteRepository : RepositoryBase<Cliente>, IClienteRepository
{
    public ClienteRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<IEnumerable<Cliente>> BuscarPorNomeAsync(string nome)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Cliente>()
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.Nome, $"%{nome}%"))
            .OrderBy(c => c.Nome)
            .ToListAsync();
    }

    public async Task<Cliente?> ObterComVendasAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Cliente>()
            .Include(c => c.Vendas).ThenInclude(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(IReadOnlyList<Cliente> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<Cliente>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(c => EF.Functions.ILike(c.Nome, $"%{termo}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderBy(c => c.Nome)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<bool> PossuiVinculosAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .IgnoreQueryFilters()
            .AnyAsync(v => v.ClienteId == clienteId, cancellationToken);
    }

    public async Task RemoverFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var cliente = await context.Set<Cliente>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cliente is null)
            return;

        context.Set<Cliente>().Remove(cliente);
        await SalvarAlteracoesAsync(context);
    }

    public async Task<bool> ExisteFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Cliente>()
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == id, cancellationToken);
    }

    public override async Task RemoverAsync(int id)
    {
        await RemoverFisicamenteAsync(id);
    }
}
