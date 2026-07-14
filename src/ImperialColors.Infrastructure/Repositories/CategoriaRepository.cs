using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class CategoriaRepository : RepositoryBase<Categoria>, IRepository<Categoria>
{
    public CategoriaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
}
