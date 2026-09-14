using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ItemOrcamentoMapping : IEntityTypeConfiguration<ItemOrcamento>
{
    public void Configure(EntityTypeBuilder<ItemOrcamento> builder)
    {
        builder.ToTable("itens_orcamento");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.OrcamentoId).HasColumnName("orcamento_id");
        builder.Property(i => i.ProdutoId).HasColumnName("produto_id");
        builder.Property(i => i.NomeProduto).HasColumnName("nome_produto").HasMaxLength(200).IsRequired();
        builder.Property(i => i.CodigoProduto).HasColumnName("codigo_produto").HasMaxLength(50);
        builder.Property(i => i.Unidade).HasColumnName("unidade").HasMaxLength(10);
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3);
        builder.Property(i => i.PrecoUnitario).HasColumnName("preco_unitario").HasPrecision(10, 2);
        builder.Property(i => i.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
        builder.Property(i => i.CriadoEm).HasColumnName("criado_em");
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(i => i.Ativo).HasColumnName("ativo");

        // SetNull pelo mesmo motivo do cliente: um produto descontinuado não pode arrastar
        // consigo os orçamentos em que apareceu.
        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.OrcamentoId);
    }
}
