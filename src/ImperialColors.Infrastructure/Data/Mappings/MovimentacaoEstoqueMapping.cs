using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class MovimentacaoEstoqueMapping : IEntityTypeConfiguration<MovimentacaoEstoque>
{
    public void Configure(EntityTypeBuilder<MovimentacaoEstoque> builder)
    {
        builder.ToTable("movimentacoes_estoque");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(m => m.ProdutoId).HasColumnName("produto_id").IsRequired();
        builder.Property(m => m.Tipo).HasColumnName("tipo").IsRequired();
        builder.Property(m => m.Quantidade).HasColumnName("quantidade").HasPrecision(10, 3);
        builder.Property(m => m.QuantidadeAnterior).HasColumnName("quantidade_anterior").HasPrecision(10, 3);
        builder.Property(m => m.QuantidadeAtual).HasColumnName("quantidade_atual").HasPrecision(10, 3);
        builder.Property(m => m.Motivo).HasColumnName("motivo").HasMaxLength(500);
        builder.Property(m => m.Usuario).HasColumnName("usuario").HasMaxLength(100);
        builder.Property(m => m.VendaId).HasColumnName("venda_id");
        builder.Property(m => m.VendaExternaId).HasColumnName("venda_externa_id");
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em");
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(m => m.Ativo).HasColumnName("ativo");

        builder.HasOne(m => m.Produto).WithMany(p => p.Movimentacoes).HasForeignKey(m => m.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Venda).WithMany(v => v.Movimentacoes).HasForeignKey(m => m.VendaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(m => m.VendaExterna).WithMany(v => v.Movimentacoes).HasForeignKey(m => m.VendaExternaId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.CriadoEm);
        builder.HasIndex(m => m.ProdutoId);
    }
}
