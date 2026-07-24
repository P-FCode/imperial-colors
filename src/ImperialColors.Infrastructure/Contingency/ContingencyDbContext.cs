using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Contingency;

public class ContingencyDbContext : DbContext
{
    public ContingencyDbContext(DbContextOptions<ContingencyDbContext> options) : base(options) { }

    public DbSet<VendaContingencia> VendasContingencia => Set<VendaContingencia>();
    public DbSet<ItemVendaContingencia> ItensVendaContingencia => Set<ItemVendaContingencia>();
    public DbSet<PagamentoContingencia> PagamentosContingencia => Set<PagamentoContingencia>();
    public DbSet<EstoqueLocalCache> EstoqueLocal => Set<EstoqueLocalCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VendaContingencia>(e =>
        {
            e.ToTable("vendas_contingencia");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ContingenciaId).IsUnique();
            e.HasIndex(x => x.PendenteSincronizacao);
            e.Property(x => x.NumeroTemporario).HasMaxLength(40).IsRequired();
            e.Property(x => x.PayloadJson).IsRequired();
            e.HasMany(x => x.Itens).WithOne(i => i.Venda!).HasForeignKey(i => i.VendaContingenciaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Pagamentos).WithOne(p => p.Venda!).HasForeignKey(p => p.VendaContingenciaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemVendaContingencia>(e =>
        {
            e.ToTable("itens_venda_contingencia");
            e.HasKey(x => x.Id);
            e.Property(x => x.NomeProduto).HasMaxLength(200);
        });

        modelBuilder.Entity<PagamentoContingencia>(e =>
        {
            e.ToTable("pagamentos_contingencia");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<EstoqueLocalCache>(e =>
        {
            e.ToTable("estoque_local_cache");
            e.HasKey(x => x.ProdutoId);
            e.Property(x => x.Nome).HasMaxLength(200).IsRequired();
            e.Property(x => x.CodigoInterno).HasMaxLength(50);
            e.Property(x => x.Unidade).HasMaxLength(10);
        });
    }
}
