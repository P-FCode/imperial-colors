using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class OrcamentoMapping : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("orcamentos");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(o => o.NumeroOrcamento).HasColumnName("numero_orcamento").HasMaxLength(30).IsRequired();
        builder.Property(o => o.ClienteId).HasColumnName("cliente_id");
        builder.Property(o => o.NomeCliente).HasColumnName("nome_cliente").HasMaxLength(200).IsRequired();
        builder.Property(o => o.TelefoneCliente).HasColumnName("telefone_cliente").HasMaxLength(30);
        builder.Property(o => o.DataOrcamento).HasColumnName("data_orcamento");
        builder.Property(o => o.DataValidade).HasColumnName("data_validade");
        builder.Property(o => o.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2);
        builder.Property(o => o.Desconto).HasColumnName("desconto").HasPrecision(12, 2);
        builder.Property(o => o.Total).HasColumnName("total").HasPrecision(12, 2);
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(o => o.Observacoes).HasColumnName("observacoes").HasMaxLength(1000);
        builder.Property(o => o.Usuario).HasColumnName("usuario").HasMaxLength(100);
        builder.Property(o => o.CriadoEm).HasColumnName("criado_em");
        builder.Property(o => o.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(o => o.Ativo).HasColumnName("ativo");

        builder.HasMany(o => o.Itens)
            .WithOne(i => i.Orcamento)
            .HasForeignKey(i => i.OrcamentoId)
            .OnDelete(DeleteBehavior.Cascade);

        // SetNull: excluir um cliente não pode apagar o histórico de propostas feitas a ele —
        // o nome já está copiado em nome_cliente e o orçamento continua legível.
        builder.HasOne(o => o.Cliente)
            .WithMany()
            .HasForeignKey(o => o.ClienteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(o => o.NumeroOrcamento).IsUnique();
        builder.HasIndex(o => o.DataOrcamento);
        builder.HasIndex(o => o.ClienteId);
        builder.HasIndex(o => o.Status);
    }
}
