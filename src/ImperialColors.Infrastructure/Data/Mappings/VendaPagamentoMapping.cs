using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class VendaPagamentoMapping : IEntityTypeConfiguration<VendaPagamento>
{
    public void Configure(EntityTypeBuilder<VendaPagamento> builder)
    {
        builder.ToTable("venda_pagamentos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(p => p.VendaId).HasColumnName("venda_id");
        builder.Property(p => p.FormaPagamento).HasColumnName("forma_pagamento");
        builder.Property(p => p.Valor).HasColumnName("valor").HasPrecision(10, 2);
        builder.Property(p => p.ValorRecebido).HasColumnName("valor_recebido").HasPrecision(10, 2);
        builder.Property(p => p.QuantidadeParcelas).HasColumnName("quantidade_parcelas").HasDefaultValue(1);
        builder.Property(p => p.Ordem).HasColumnName("ordem");
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em");
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(p => p.Ativo).HasColumnName("ativo");

        builder.HasIndex(p => p.VendaId);
        builder.HasOne(p => p.Venda)
            .WithMany(v => v.Pagamentos)
            .HasForeignKey(p => p.VendaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
