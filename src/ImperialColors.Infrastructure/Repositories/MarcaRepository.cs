using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class MarcaRepository : RepositoryBase<Marca>, IRepository<Marca>
{
    public MarcaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }
}
