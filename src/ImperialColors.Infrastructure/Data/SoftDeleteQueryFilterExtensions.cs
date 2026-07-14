using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ImperialColors.Infrastructure.Data;

internal static class SoftDeleteQueryFilterExtensions
{
    public static void AplicarFiltroSoftDelete(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var method = typeof(SoftDeleteQueryFilterExtensions)
                .GetMethod(nameof(CriarFiltroAtivo), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            var filter = (LambdaExpression)method.Invoke(null, null)!;
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }
    }

    private static Expression<Func<TEntity, bool>> CriarFiltroAtivo<TEntity>()
        where TEntity : BaseEntity
        => entity => entity.Ativo;
}
