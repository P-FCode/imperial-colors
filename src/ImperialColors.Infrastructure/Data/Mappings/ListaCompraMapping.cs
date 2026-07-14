using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ListaCompraMapping : IEntityTypeConfiguration<ListaCompra>
{
    public void Configure(EntityTypeBuilder<ListaCompra> builder)
    {
        builder.ToTable("listas_compra");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(l => l.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(l => l.FornecedorId).HasColumnName("fornecedor_id");
        builder.Property(l => l.Finalizada).HasColumnName("finalizada");
        builder.Property(l => l.Observacoes).HasColumnName("observacoes");
        builder.Property(l => l.NotaFiscalConteudo).HasColumnName("nota_fiscal_conteudo").HasColumnType("bytea");
        builder.Property(l => l.NotaFiscalNomeArquivo).HasColumnName("nota_fiscal_nome_arquivo").HasMaxLength(260);
        builder.Property(l => l.CriadoEm).HasColumnName("criado_em");
        builder.Property(l => l.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(l => l.Ativo).HasColumnName("ativo");

        builder.HasOne(l => l.Fornecedor).WithMany(f => f.ListasCompra).HasForeignKey(l => l.FornecedorId).OnDelete(DeleteBehavior.SetNull);
    }
}
