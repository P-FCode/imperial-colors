using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ItemNotaFiscalMapping : IEntityTypeConfiguration<ItemNotaFiscal>
{
    public void Configure(EntityTypeBuilder<ItemNotaFiscal> builder)
    {
        builder.ToTable("itens_nota_fiscal");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(i => i.NotaFiscalId).HasColumnName("nota_fiscal_id").IsRequired();
        builder.Property(i => i.ProdutoId).HasColumnName("produto_id");
        builder.Property(i => i.NItem).HasColumnName("n_item");

        builder.Property(i => i.CodigoProduto).HasColumnName("codigo_produto").HasMaxLength(60).IsRequired();
        builder.Property(i => i.CodigoBarras).HasColumnName("codigo_barras").HasMaxLength(20);
        builder.Property(i => i.Descricao).HasColumnName("descricao").HasMaxLength(120).IsRequired();
        builder.Property(i => i.Ncm).HasColumnName("ncm").HasMaxLength(8);
        builder.Property(i => i.Cest).HasColumnName("cest").HasMaxLength(7);
        builder.Property(i => i.Cfop).HasColumnName("cfop").HasMaxLength(4).IsRequired();
        builder.Property(i => i.Unidade).HasColumnName("unidade").HasMaxLength(6);
        builder.Property(i => i.Quantidade).HasColumnName("quantidade").HasPrecision(12, 4);
        builder.Property(i => i.ValorUnitario).HasColumnName("valor_unitario").HasPrecision(14, 4);
        builder.Property(i => i.ValorTotal).HasColumnName("valor_total").HasPrecision(12, 2);
        builder.Property(i => i.UnidadeTributavel).HasColumnName("unidade_tributavel").HasMaxLength(6);
        builder.Property(i => i.QuantidadeTributavel).HasColumnName("quantidade_tributavel").HasPrecision(12, 4);
        builder.Property(i => i.ValorUnitarioTributavel).HasColumnName("valor_unitario_tributavel").HasPrecision(14, 4);
        builder.Property(i => i.CompoeTotalNota).HasColumnName("compoe_total_nota").HasDefaultValue(true);

        builder.Property(i => i.Origem).HasColumnName("origem").HasMaxLength(1);
        builder.Property(i => i.CstIcms).HasColumnName("cst_icms").HasMaxLength(2);
        builder.Property(i => i.CsosnIcms).HasColumnName("csosn_icms").HasMaxLength(3);
        builder.Property(i => i.BaseIcms).HasColumnName("base_icms").HasPrecision(12, 2);
        builder.Property(i => i.ReducaoBaseCalculo).HasColumnName("reducao_base_calculo").HasPrecision(5, 2);
        builder.Property(i => i.AliquotaIcms).HasColumnName("aliquota_icms").HasPrecision(6, 4);
        builder.Property(i => i.ValorIcms).HasColumnName("valor_icms").HasPrecision(12, 2);
        builder.Property(i => i.Mva).HasColumnName("mva").HasPrecision(7, 4);
        builder.Property(i => i.BaseIcmsSt).HasColumnName("base_icms_st").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaIcmsSt).HasColumnName("aliquota_icms_st").HasPrecision(6, 4);
        builder.Property(i => i.ValorIcmsSt).HasColumnName("valor_icms_st").HasPrecision(12, 2);
        builder.Property(i => i.BaseIcmsStRetido).HasColumnName("base_icms_st_retido").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaIcmsStRetido).HasColumnName("aliquota_icms_st_retido").HasPrecision(6, 4);
        builder.Property(i => i.ValorIcmsStRetido).HasColumnName("valor_icms_st_retido").HasPrecision(12, 2);

        builder.Property(i => i.CstPis).HasColumnName("cst_pis").HasMaxLength(2);
        builder.Property(i => i.BasePis).HasColumnName("base_pis").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaPis).HasColumnName("aliquota_pis").HasPrecision(6, 4);
        builder.Property(i => i.ValorPis).HasColumnName("valor_pis").HasPrecision(12, 2);
        builder.Property(i => i.CstCofins).HasColumnName("cst_cofins").HasMaxLength(2);
        builder.Property(i => i.BaseCofins).HasColumnName("base_cofins").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaCofins).HasColumnName("aliquota_cofins").HasPrecision(6, 4);
        builder.Property(i => i.ValorCofins).HasColumnName("valor_cofins").HasPrecision(12, 2);

        builder.Property(i => i.CstIpi).HasColumnName("cst_ipi").HasMaxLength(2);
        builder.Property(i => i.BaseIpi).HasColumnName("base_ipi").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaIpi).HasColumnName("aliquota_ipi").HasPrecision(6, 4);
        builder.Property(i => i.ValorIpi).HasColumnName("valor_ipi").HasPrecision(12, 2);
        builder.Property(i => i.CodigoEnquadramentoIpi).HasColumnName("codigo_enquadramento_ipi").HasMaxLength(3);

        builder.Property(i => i.CstIbsCbs).HasColumnName("cst_ibs_cbs").HasMaxLength(3);
        builder.Property(i => i.CClassTrib).HasColumnName("c_class_trib").HasMaxLength(6);
        builder.Property(i => i.BaseIbsCbs).HasColumnName("base_ibs_cbs").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaIbsUf).HasColumnName("aliquota_ibs_uf").HasPrecision(6, 4);
        builder.Property(i => i.ValorIbsUf).HasColumnName("valor_ibs_uf").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaIbsMunicipio).HasColumnName("aliquota_ibs_municipio").HasPrecision(6, 4);
        builder.Property(i => i.ValorIbsMunicipio).HasColumnName("valor_ibs_municipio").HasPrecision(12, 2);
        builder.Property(i => i.AliquotaCbs).HasColumnName("aliquota_cbs").HasPrecision(6, 4);
        builder.Property(i => i.ValorCbs).HasColumnName("valor_cbs").HasPrecision(12, 2);

        builder.Property(i => i.CriadoEm).HasColumnName("criado_em");
        builder.Property(i => i.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(i => i.Ativo).HasColumnName("ativo");

        builder.HasOne(i => i.NotaFiscal).WithMany(n => n.Itens).HasForeignKey(i => i.NotaFiscalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Produto).WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.SetNull);
    }
}
