using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class TrocaMapping : IEntityTypeConfiguration<Troca>
{
    public void Configure(EntityTypeBuilder<Troca> builder)
    {
        builder.ToTable("trocas");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(t => t.VendaOrigemId).HasColumnName("venda_origem_id");
        builder.Property(t => t.VendaExternaOrigemId).HasColumnName("venda_externa_origem_id");
        builder.Property(t => t.ProdutoDevolvidoId).HasColumnName("produto_devolvido_id").IsRequired();
        builder.Property(t => t.QuantidadeDevolvida).HasColumnName("quantidade_devolvida").HasPrecision(10, 3);
        builder.Property(t => t.ValorUnitarioDevolucao).HasColumnName("valor_unitario_devolucao").HasPrecision(10, 2);
        builder.Property(t => t.RetornarAoEstoque).HasColumnName("retornar_ao_estoque");
        builder.Property(t => t.ProdutoNovoId).HasColumnName("produto_novo_id").IsRequired();
        builder.Property(t => t.QuantidadeNova).HasColumnName("quantidade_nova").HasPrecision(10, 3);
        builder.Property(t => t.ValorUnitarioNovo).HasColumnName("valor_unitario_novo").HasPrecision(10, 2);
        builder.Property(t => t.FormaPagamentoDiferenca).HasColumnName("forma_pagamento_diferenca");
        builder.Property(t => t.Observacoes).HasColumnName("observacoes").HasMaxLength(500);
        builder.Property(t => t.Usuario).HasColumnName("usuario").HasMaxLength(100);
        builder.Property(t => t.DataTroca).HasColumnName("data_troca");
        builder.Property(t => t.CriadoEm).HasColumnName("criado_em");
        builder.Property(t => t.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(t => t.Ativo).HasColumnName("ativo");

        builder.HasOne(t => t.VendaOrigem)
            .WithMany()
            .HasForeignKey(t => t.VendaOrigemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.VendaExternaOrigem)
            .WithMany()
            .HasForeignKey(t => t.VendaExternaOrigemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProdutoDevolvido)
            .WithMany()
            .HasForeignKey(t => t.ProdutoDevolvidoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProdutoNovo)
            .WithMany()
            .HasForeignKey(t => t.ProdutoNovoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(t => t.ValorTotalDevolvido);
        builder.Ignore(t => t.ValorTotalNovo);
        builder.Ignore(t => t.DiferencaValor);
    }
}
