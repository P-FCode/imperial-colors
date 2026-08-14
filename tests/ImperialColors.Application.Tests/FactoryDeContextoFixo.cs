using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Devolve sempre o mesmo contexto (o da transação do teste), ignorando o descarte — os
/// repositórios abrem o próprio contexto pelo factory e fazem <c>await using</c> nele, então
/// sem isto nenhum teste de integração conseguiria exercitar a consulta real dentro de uma
/// transação que ele mesmo controla e reverte no fim.
/// </summary>
internal sealed class FactoryDeContextoFixo(AppDbContext contexto) : IDbContextFactory<AppDbContext>
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
