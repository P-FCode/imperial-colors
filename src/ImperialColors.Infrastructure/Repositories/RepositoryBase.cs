using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ImperialColors.Infrastructure.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IDbContextFactory<AppDbContext> ContextFactory;
    private readonly ILogger? _logger;

    public RepositoryBase(IDbContextFactory<AppDbContext> contextFactory, ILogger? logger = null)
    {
        ContextFactory = contextFactory;
        _logger = logger;
    }

    public virtual async Task<T?> ObterPorIdAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> ObterTodosAsync()
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AdicionarAsync(T entity)
    {
        await using var context = ContextFactory.CreateDbContext();
        await context.Set<T>().AddAsync(entity);
        await SalvarAlteracoesAsync(context);
        return entity;
    }

    public virtual async Task<T> AtualizarAsync(T entity)
    {
        await using var context = ContextFactory.CreateDbContext();
        context.Set<T>().Update(entity);
        await SalvarAlteracoesAsync(context);
        return entity;
    }

    public virtual async Task RemoverAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        var entity = await context.Set<T>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null || !entity.Ativo)
            return;

        entity.Ativo = false;
        entity.AtualizadoEm = DateTime.UtcNow;
        await SalvarAlteracoesAsync(context);
    }

    public virtual async Task<bool> ExisteAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AnyAsync(e => e.Id == id);
    }

    public virtual async Task<int> ContarAsync(Expression<Func<T, bool>>? predicate = null)
    {
        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<T>().AsQueryable();
        return predicate is null
            ? await query.CountAsync()
            : await query.CountAsync(predicate);
    }

    protected static void Desanexar(AppDbContext context, object entity)
    {
        var entry = context.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    protected async Task SalvarAlteracoesAsync(AppDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var detalhe = DatabaseExceptionHelper.ObterMensagemDetalhada(ex);
            _logger?.LogError(ex, "Erro ao persistir {Entidade}: {Detalhe}", typeof(T).Name, detalhe);
            throw new DomainException($"Erro real do banco: {detalhe}", ex);
        }
    }
}
