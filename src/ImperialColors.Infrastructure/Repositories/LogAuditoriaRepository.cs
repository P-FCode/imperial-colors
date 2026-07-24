using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class LogAuditoriaRepository : ILogAuditoriaRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public LogAuditoriaRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task AdicionarAsync(LogAuditoria log, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        ctx.LogsAuditoria.Add(log);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<LogAuditoria?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        return await ctx.LogsAuditoria.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<LogAuditoria> Itens, int Total)> ObterPaginadoAsync(
        DateTime? dataInicio,
        DateTime? dataFim,
        NivelLogAuditoria? nivel,
        string? modulo,
        string? termoBusca,
        int pagina,
        int itensPorPagina,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        var query = ctx.LogsAuditoria.AsNoTracking().AsQueryable();

        if (dataInicio.HasValue)
        {
            var inicio = dataInicio.Value.Date;
            query = query.Where(l => l.DataHora >= inicio);
        }

        if (dataFim.HasValue)
        {
            var fimExclusivo = dataFim.Value.Date.AddDays(1);
            query = query.Where(l => l.DataHora < fimExclusivo);
        }

        if (nivel.HasValue)
            query = query.Where(l => l.Nivel == nivel.Value);

        if (!string.IsNullOrWhiteSpace(modulo) &&
            !string.Equals(modulo, "Todos", StringComparison.OrdinalIgnoreCase))
        {
            var moduloNorm = modulo.Trim();
            query = query.Where(l => l.Modulo == moduloNorm);
        }

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.NomeUsuario, $"%{termo}%") ||
                EF.Functions.ILike(l.Descricao, $"%{termo}%") ||
                EF.Functions.ILike(l.Acao, $"%{termo}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderByDescending(l => l.DataHora)
            .ThenByDescending(l => l.Id)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }
}
