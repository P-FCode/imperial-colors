using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ItemListaCompraMapping : IEntityTypeConfiguration<ItemListaCompra>
{
    public void Configure(EntityTypeBuilder<ItemListaCompra> builder)
    {
        builder.ToTable("itens_lista_compra");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.ListaCompraId).HasColumnName("lista_compra_id").IsRequired();
        builder.Property(i => i.ProdutoId).HasColumnName("produto_id");
        builder.Property(i => i.DescricaoItem).HasColumnName("descricao_item").HasMaxLength(200);
        builder.Property(i => i.QuantidadeDesejada).HasColumnName("quantidade_desejada").HasPrecision(10, 3);
        builder.Property(i => i.QuantidadeComprada).HasColumnName("quantidade_comprada").HasPrecision(10, 3);
        builder.Property(i => i.Comprado).HasColumnName("comprado");
        builder.Property(i => i.Observacoes).HasColumnName("observacoes");
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em");
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(i => i.Ativo).HasColumnName("ativo");

        builder.HasOne(i => i.ListaCompra).WithMany(l => l.Itens).HasForeignKey(i => i.ListaCompraId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Produto).WithMany(p => p.ItensListaCompra).HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.SetNull);
    }
}
