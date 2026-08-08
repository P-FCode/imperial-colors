using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class InscricaoEstadualSubstitutoMapping : IEntityTypeConfiguration<InscricaoEstadualSubstituto>
{
    public void Configure(EntityTypeBuilder<InscricaoEstadualSubstituto> builder)
    {
        builder.ToTable("inscricoes_estaduais_substituto");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(i => i.ConfiguracaoFiscalEmpresaId).HasColumnName("configuracao_fiscal_empresa_id");
        builder.Property(i => i.Uf).HasColumnName("uf").HasMaxLength(2).IsRequired();
        builder.Property(i => i.InscricaoEstadual).HasColumnName("inscricao_estadual").HasMaxLength(20).IsRequired();

        builder.HasIndex(i => new { i.ConfiguracaoFiscalEmpresaId, i.Uf }).IsUnique();
    }
}
