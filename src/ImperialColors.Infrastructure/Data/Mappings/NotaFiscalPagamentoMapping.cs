using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class NotaFiscalPagamentoMapping : IEntityTypeConfiguration<NotaFiscalPagamento>
{
    public void Configure(EntityTypeBuilder<NotaFiscalPagamento> builder)
    {
        builder.ToTable("nota_fiscal_pagamentos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(p => p.NotaFiscalId).HasColumnName("nota_fiscal_id").IsRequired();
        builder.Property(p => p.FormaPagamento).HasColumnName("forma_pagamento");
        builder.Property(p => p.Valor).HasColumnName("valor").HasPrecision(12, 2);
        builder.Property(p => p.QuantidadeParcelas).HasColumnName("quantidade_parcelas").HasDefaultValue(1);
        builder.Property(p => p.Ordem).HasColumnName("ordem");

        builder.Property(p => p.CriadoEm).HasColumnName("criado_em");
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.Ativo).HasColumnName("ativo");

        builder.HasOne(p => p.NotaFiscal).WithMany(n => n.Pagamentos).HasForeignKey(p => p.NotaFiscalId).OnDelete(DeleteBehavior.Cascade);
    }
}
