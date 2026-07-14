using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class ParametroSistemaRepository : IParametroSistemaRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ParametroSistemaRepository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<DateTime?> ObterDataAsync(string chave, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var parametro = await context.Set<ParametroSistema>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Chave == chave, cancellationToken);

        return parametro?.ValorData;
    }

    public async Task SalvarDataAsync(string chave, DateTime valor, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var parametro = await context.Set<ParametroSistema>()
            .FirstOrDefaultAsync(p => p.Chave == chave, cancellationToken);

        if (parametro is null)
        {
            parametro = new ParametroSistema
            {
                Chave = chave,
                ValorData = valor.Date,
                CriadoEm = DateTime.UtcNow
            };
            await context.Set<ParametroSistema>().AddAsync(parametro, cancellationToken);
        }
        else
        {
            parametro.ValorData = valor.Date;
            parametro.AtualizadoEm = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
