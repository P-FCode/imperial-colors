using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Produto> Produtos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Marca> Marcas { get; set; }
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<Venda> Vendas { get; set; }
    public DbSet<ItemVenda> ItensVenda { get; set; }
    public DbSet<VendaPagamento> VendaPagamentos { get; set; }
    public DbSet<Fornecedor> Fornecedores { get; set; }
    public DbSet<ListaCompra> ListasCompra { get; set; }
    public DbSet<ItemListaCompra> ItensListaCompra { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Troca> Trocas { get; set; }
    public DbSet<VendaExterna> VendasExternas { get; set; }
    public DbSet<ItemVendaExterna> ItensVendaExterna { get; set; }
    public DbSet<ParametroSistema> ParametrosSistema { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Npgsql 6+ mapeia DateTime para timestamptz (requer UTC).
        // Configurar todas as propriedades DateTime para timestamp (sem fuso) evita
        // erros "Cannot write DateTime with Kind=Local" sem precisar converter em cada query.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                    property.SetColumnType("timestamp without time zone");
                else if (property.ClrType == typeof(DateTime?))
                    property.SetColumnType("timestamp without time zone");
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.AplicarFiltroSoftDelete();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AtualizarTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AtualizarTimestamps();
        return base.SaveChanges();
    }

    private void AtualizarTimestamps()
    {
        var entries = ChangeTracker.Entries<Domain.Entities.BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.AtualizadoEm = DateTime.UtcNow;
        }
    }
}
