using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class TributacaoCategoriaMapping : IEntityTypeConfiguration<TributacaoCategoria>
{
    public void Configure(EntityTypeBuilder<TributacaoCategoria> builder)
    {
        builder.ToTable("tributacao_categorias");

        builder.HasKey(t => t.CategoriaId);
        builder.Property(t => t.CategoriaId).HasColumnName("categoria_id").ValueGeneratedNever();

        builder.Property(t => t.Ncm).HasColumnName("ncm").HasMaxLength(8);
        builder.Property(t => t.Cest).HasColumnName("cest").HasMaxLength(7);
        builder.Property(t => t.Origem).HasColumnName("origem");

        builder.Property(t => t.CstIcms).HasColumnName("cst_icms").HasMaxLength(2);
        builder.Property(t => t.CsosnIcms).HasColumnName("csosn_icms").HasMaxLength(3);
        builder.Property(t => t.AliquotaIcms).HasColumnName("aliquota_icms").HasPrecision(5, 2);
        builder.Property(t => t.AliquotaIcmsSt).HasColumnName("aliquota_icms_st").HasPrecision(5, 2);
        builder.Property(t => t.Mva).HasColumnName("mva").HasPrecision(6, 2);
        builder.Property(t => t.ReducaoBaseCalculo).HasColumnName("reducao_base_calculo").HasPrecision(5, 2);

        builder.Property(t => t.CstPis).HasColumnName("cst_pis").HasMaxLength(2);
        builder.Property(t => t.AliquotaPis).HasColumnName("aliquota_pis").HasPrecision(5, 2);

        builder.Property(t => t.CstCofins).HasColumnName("cst_cofins").HasMaxLength(2);
        builder.Property(t => t.AliquotaCofins).HasColumnName("aliquota_cofins").HasPrecision(5, 2);

        builder.Property(t => t.CstIpi).HasColumnName("cst_ipi").HasMaxLength(2);
        builder.Property(t => t.CodigoEnquadramentoIpi).HasColumnName("codigo_enquadramento_ipi").HasMaxLength(3);
        builder.Property(t => t.AliquotaIpi).HasColumnName("aliquota_ipi").HasPrecision(5, 2);

        builder.Property(t => t.CfopDentroEstado).HasColumnName("cfop_dentro_estado").HasMaxLength(4);
        builder.Property(t => t.CfopForaEstado).HasColumnName("cfop_fora_estado").HasMaxLength(4);

        builder.Property(t => t.CstIbsCbs).HasColumnName("cst_ibs_cbs").HasMaxLength(3);
        builder.Property(t => t.CClassTrib).HasColumnName("c_class_trib").HasMaxLength(6);
        builder.Property(t => t.CstIS).HasColumnName("cst_is").HasMaxLength(3);
        builder.Property(t => t.CClassTribIS).HasColumnName("c_class_trib_is").HasMaxLength(6);
        builder.Property(t => t.AliquotaIS).HasColumnName("aliquota_is").HasPrecision(5, 2);
        builder.Property(t => t.AliquotaIbsMunicipioDiferimento).HasColumnName("aliquota_ibs_municipio_diferimento").HasPrecision(5, 2);
        builder.Property(t => t.AliquotaIbsMunicipioReducao).HasColumnName("aliquota_ibs_municipio_reducao").HasPrecision(5, 2);

        builder.Property(t => t.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasOne(t => t.Categoria)
            .WithOne(c => c.TributacaoPadrao)
            .HasForeignKey<TributacaoCategoria>(t => t.CategoriaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
