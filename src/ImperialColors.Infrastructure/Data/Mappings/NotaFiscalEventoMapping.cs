using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class NotaFiscalEventoMapping : IEntityTypeConfiguration<NotaFiscalEvento>
{
    public void Configure(EntityTypeBuilder<NotaFiscalEvento> builder)
    {
        builder.ToTable("nota_fiscal_eventos");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(e => e.NotaFiscalId).HasColumnName("nota_fiscal_id").IsRequired();
        builder.Property(e => e.Tipo).HasColumnName("tipo");
        builder.Property(e => e.DataHora).HasColumnName("data_hora");
        builder.Property(e => e.Sucesso).HasColumnName("sucesso");
        builder.Property(e => e.NProtEvento).HasColumnName("n_prot_evento").HasMaxLength(20);
        builder.Property(e => e.CStat).HasColumnName("c_stat").HasMaxLength(5);
        builder.Property(e => e.XMotivo).HasColumnName("x_motivo").HasMaxLength(500);
        builder.Property(e => e.Texto).HasColumnName("texto").HasMaxLength(1000);
        builder.Property(e => e.Sequencial).HasColumnName("sequencial");
        builder.Property(e => e.TraceId).HasColumnName("trace_id").HasMaxLength(100);
        builder.Property(e => e.Usuario).HasColumnName("usuario").HasMaxLength(100);

        builder.Property(e => e.CriadoEm).HasColumnName("criado_em");
        builder.Property(e => e.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(e => e.Ativo).HasColumnName("ativo");

        builder.HasIndex(e => e.NotaFiscalId);
        builder.HasOne(e => e.NotaFiscal).WithMany(n => n.Eventos).HasForeignKey(e => e.NotaFiscalId).OnDelete(DeleteBehavior.Cascade);
    }
}
