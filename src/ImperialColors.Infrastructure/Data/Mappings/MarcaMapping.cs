using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class MarcaMapping : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        builder.ToTable("marcas");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(m => m.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(m => m.Descricao).HasColumnName("descricao");
        builder.Property(m => m.CriadoEm).HasColumnName("criado_em");
        builder.Property(m => m.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(m => m.Ativo).HasColumnName("ativo");

        builder.HasIndex(m => m.Nome).IsUnique();
    }
}
