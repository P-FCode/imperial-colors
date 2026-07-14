using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class CategoriaMapping : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Descricao).HasColumnName("descricao");
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em");
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(c => c.Ativo).HasColumnName("ativo");

        builder.HasIndex(c => c.Nome).IsUnique();
    }
}
