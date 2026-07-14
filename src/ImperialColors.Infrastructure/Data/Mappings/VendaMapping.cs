using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class VendaMapping : IEntityTypeConfiguration<Venda>
{
    public void Configure(EntityTypeBuilder<Venda> builder)
    {
        builder.ToTable("vendas");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(v => v.NumeroVenda).HasColumnName("numero_venda").HasMaxLength(20).IsRequired();
        builder.Property(v => v.ClienteId).HasColumnName("cliente_id");
        builder.Property(v => v.Status).HasColumnName("status");
        builder.Property(v => v.Subtotal).HasColumnName("subtotal").HasPrecision(10, 2);
        builder.Property(v => v.Desconto).HasColumnName("desconto").HasPrecision(10, 2);
        builder.Property(v => v.Total).HasColumnName("total").HasPrecision(10, 2);
        builder.Property(v => v.FormaPagamento).HasColumnName("forma_pagamento");
        builder.Property(v => v.QuantidadeParcelas).HasColumnName("quantidade_parcelas").HasDefaultValue(1);
        builder.Property(v => v.ValorPago).HasColumnName("valor_pago").HasPrecision(10, 2);
        builder.Property(v => v.Troco).HasColumnName("troco").HasPrecision(10, 2);
        builder.Property(v => v.NomeCompradorCupom).HasColumnName("nome_comprador_cupom").HasMaxLength(200);
        builder.Property(v => v.DocumentoCompradorCupom).HasColumnName("documento_comprador_cupom").HasMaxLength(20);
        builder.Property(v => v.TipoPessoaComprador).HasColumnName("tipo_pessoa_comprador");
        builder.Property(v => v.Observacoes).HasColumnName("observacoes");
        builder.Property(v => v.Usuario).HasColumnName("usuario").HasMaxLength(100);
        builder.Property(v => v.DataVenda).HasColumnName("data_venda");
        builder.Property(v => v.CriadoEm).HasColumnName("criado_em");
        builder.Property(v => v.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(v => v.Ativo).HasColumnName("ativo");

        builder.HasIndex(v => v.NumeroVenda).IsUnique();
        builder.HasIndex(v => v.DataVenda);
        builder.HasIndex(v => v.Status).HasDatabaseName("IX_vendas_status");
        builder.HasOne(v => v.Cliente).WithMany(c => c.Vendas).HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.SetNull);
    }
}
