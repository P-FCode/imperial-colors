using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ParametroSistemaMapping : IEntityTypeConfiguration<ParametroSistema>
{
    public void Configure(EntityTypeBuilder<ParametroSistema> builder)
    {
        builder.ToTable("parametros_sistema");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(p => p.Chave).HasColumnName("chave").HasMaxLength(100).IsRequired();
        builder.Property(p => p.ValorData).HasColumnName("valor_data");
        builder.Property(p => p.ValorTexto).HasColumnName("valor_texto").HasMaxLength(500);
        builder.Property(p => p.CriadoEm).HasColumnName("criado_em");
        builder.Property(p => p.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasIndex(p => p.Chave).IsUnique();
    }
}
