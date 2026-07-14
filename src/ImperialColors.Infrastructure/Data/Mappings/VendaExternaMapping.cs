using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class VendaExternaMapping : IEntityTypeConfiguration<VendaExterna>
{
    public void Configure(EntityTypeBuilder<VendaExterna> builder)
    {
        builder.ToTable("vendas_externas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(v => v.NumeroVendaExterna).HasColumnName("numero_venda_externa").HasMaxLength(30).IsRequired();
        builder.Property(v => v.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
        builder.Property(v => v.Total).HasColumnName("total").HasPrecision(12, 2);
        builder.Property(v => v.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
        builder.Property(v => v.Usuario).HasColumnName("usuario").HasMaxLength(100);
        builder.Property(v => v.DataVenda).HasColumnName("data_venda");
        builder.Property(v => v.CriadoEm).HasColumnName("criado_em");
        builder.Property(v => v.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(v => v.Ativo).HasColumnName("ativo");

        builder.HasMany(v => v.Itens)
            .WithOne(i => i.VendaExterna)
            .HasForeignKey(i => i.VendaExternaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Movimentacoes)
            .WithOne(m => m.VendaExterna)
            .HasForeignKey(m => m.VendaExternaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(v => v.NumeroVendaExterna).IsUnique();
        builder.HasIndex(v => v.DataVenda);
    }
}
