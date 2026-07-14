using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class UsuarioMapping : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").HasColumnType("uuid");
        builder.Property(u => u.NomeCompleto).HasColumnName("nome_completo").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
        builder.Property(u => u.SenhaHash).HasColumnName("senha_hash").HasMaxLength(512).IsRequired();
        builder.Property(u => u.Salt).HasColumnName("salt").HasMaxLength(128).IsRequired();
        builder.Property(u => u.Status).HasColumnName("status");
        builder.Property(u => u.Permissao).HasColumnName("permissao");
        builder.Property(u => u.DataCadastro).HasColumnName("data_cadastro");

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
