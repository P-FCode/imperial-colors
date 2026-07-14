using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ItemVendaExternaMapping : IEntityTypeConfiguration<ItemVendaExterna>
{
    public void Configure(EntityTypeBuilder<ItemVendaExterna> builder)
    {
        builder.ToTable("itens_venda_externa");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.VendaExternaId).HasColumnName("venda_externa_id").IsRequired();
        builder.Property(i => i.ProdutoId).HasColumnName("produto_id");
        builder.Property(i => i.NomeProduto).HasColumnName("nome_produto").HasMaxLength(200).IsRequired();
        builder.Property(i => i.CodigoBarras).HasColumnName("codigo_barras").HasMaxLength(50);
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3);
        builder.Property(i => i.PrecoBase).HasColumnName("preco_base").HasPrecision(10, 2);
        builder.Property(i => i.PrecoUnitario).HasColumnName("preco_unitario").HasPrecision(10, 2);
        builder.Property(i => i.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em");
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(i => i.Ativo).HasColumnName("ativo");

        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.VendaExternaId);
        builder.HasIndex(i => i.ProdutoId);
    }
}
