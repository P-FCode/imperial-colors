using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ItemVendaMapping : IEntityTypeConfiguration<ItemVenda>
{
    public void Configure(EntityTypeBuilder<ItemVenda> builder)
    {
        builder.ToTable("itens_venda");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.VendaId).HasColumnName("venda_id").IsRequired();
        builder.Property(i => i.ProdutoId).HasColumnName("produto_id").IsRequired();
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3);
        builder.Property(i => i.PrecoUnitario).HasColumnName("preco_unitario").HasPrecision(10, 2);
        builder.Property(i => i.Desconto).HasColumnName("desconto").HasPrecision(10, 2);
        builder.Property(i => i.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2);
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em");
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(i => i.Ativo).HasColumnName("ativo");

        builder.HasOne(i => i.Venda).WithMany(v => v.Itens).HasForeignKey(i => i.VendaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Produto).WithMany(p => p.ItensVenda).HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
    }
}
