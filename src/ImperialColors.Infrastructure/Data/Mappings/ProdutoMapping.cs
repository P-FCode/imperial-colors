using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ProdutoMapping : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(p => p.CodigoInterno).HasColumnName("codigo_interno").HasMaxLength(50).IsRequired();
        builder.Property(p => p.CodigoBarras).HasColumnName("codigo_barras").HasMaxLength(50);
        builder.Property(p => p.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(p => p.CategoriaId).HasColumnName("categoria_id");
        builder.Property(p => p.MarcaId).HasColumnName("marca_id");
        builder.Property(p => p.QuantidadeEstoque).HasColumnName("quantidade_estoque").HasPrecision(10, 3);
        builder.Property(p => p.EstoqueMinimo).HasColumnName("estoque_minimo").HasPrecision(10, 3);
        builder.Property(p => p.Unidade).HasColumnName("unidade").HasMaxLength(10);
        builder.Property(p => p.LitragemGl).HasColumnName("litragem_gl").HasPrecision(6, 2).IsRequired(false);
        builder.Property(p => p.Custo).HasColumnName("custo").HasPrecision(10, 2).IsRequired(false);
        builder.Property(p => p.PrecoVenda).HasColumnName("preco_venda").HasPrecision(10, 2);
        builder.Property(p => p.PromocaoAtiva).HasColumnName("promocao_ativa");
        builder.Property(p => p.PrecoPromocional).HasColumnName("preco_promocional").HasPrecision(10, 2);
        builder.Property(p => p.DataValidade).HasColumnName("data_validade");
        builder.Property(p => p.FornecedorId).HasColumnName("fornecedor_id");
        builder.Property(p => p.Observacoes).HasColumnName("observacoes");

        builder.Property(p => p.CriadoEm).HasColumnName("criado_em");
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.Ativo).HasColumnName("ativo");

        builder.HasIndex(p => p.CodigoInterno).IsUnique();
        builder.HasIndex(p => p.CodigoBarras);
        builder.HasIndex(p => p.Nome);
        builder.HasIndex(p => p.FornecedorId);
        builder.HasIndex(p => p.PromocaoAtiva);

        builder.HasOne(p => p.Categoria).WithMany(c => c.Produtos).HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Marca).WithMany(m => m.Produtos).HasForeignKey(p => p.MarcaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Fornecedor).WithMany(f => f.Produtos).HasForeignKey(p => p.FornecedorId).OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(p => p.EstoqueBaixo);
        builder.Ignore(p => p.SemEstoque);
    }
}
