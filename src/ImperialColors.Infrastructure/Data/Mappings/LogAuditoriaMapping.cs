using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class LogAuditoriaMapping : IEntityTypeConfiguration<LogAuditoria>
{
    public void Configure(EntityTypeBuilder<LogAuditoria> builder)
    {
        builder.ToTable("logs_auditoria");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(l => l.DataHora).HasColumnName("data_hora").IsRequired();
        builder.Property(l => l.UsuarioId).HasColumnName("usuario_id");
        builder.Property(l => l.NomeUsuario).HasColumnName("nome_usuario").HasMaxLength(120).IsRequired();
        builder.Property(l => l.Modulo).HasColumnName("modulo").HasMaxLength(80).IsRequired();
        builder.Property(l => l.Acao).HasColumnName("acao").HasMaxLength(120).IsRequired();
        builder.Property(l => l.Descricao).HasColumnName("descricao").HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Nivel).HasColumnName("nivel").IsRequired();
        builder.Property(l => l.PayloadJson).HasColumnName("payload_json");

        builder.HasIndex(l => l.DataHora).HasDatabaseName("IX_logs_auditoria_data_hora");
        builder.HasIndex(l => l.Nivel).HasDatabaseName("IX_logs_auditoria_nivel");
        builder.HasIndex(l => l.Modulo).HasDatabaseName("IX_logs_auditoria_modulo");
        builder.HasIndex(l => new { l.DataHora, l.Nivel, l.Modulo })
            .HasDatabaseName("IX_logs_auditoria_filtros");
    }
}
