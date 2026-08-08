using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class NaturezaOperacaoMapping : IEntityTypeConfiguration<NaturezaOperacao>
{
    public void Configure(EntityTypeBuilder<NaturezaOperacao> builder)
    {
        builder.ToTable("naturezas_operacao");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(n => n.Descricao).HasColumnName("descricao").HasMaxLength(150).IsRequired();
        builder.Property(n => n.TipoOperacao).HasColumnName("tipo_operacao");
        builder.Property(n => n.Finalidade).HasColumnName("finalidade");
        builder.Property(n => n.ConsumidorFinal).HasColumnName("consumidor_final").HasDefaultValue(true);
        builder.Property(n => n.Serie).HasColumnName("serie").HasMaxLength(3);

        builder.Property(n => n.CsosnPadrao).HasColumnName("csosn_padrao").HasMaxLength(3);
        builder.Property(n => n.CstIcmsPadrao).HasColumnName("cst_icms_padrao").HasMaxLength(2);

        builder.Property(n => n.CfopDentroEstado).HasColumnName("cfop_dentro_estado").HasMaxLength(4);
        builder.Property(n => n.CfopForaEstado).HasColumnName("cfop_fora_estado").HasMaxLength(4);

        builder.Property(n => n.DifalNaoContribuinte).HasColumnName("difal_nao_contribuinte");
        builder.Property(n => n.ObservacoesPadrao).HasColumnName("observacoes_padrao");

        builder.Property(n => n.CriadoEm).HasColumnName("criado_em");
        builder.Property(n => n.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(n => n.Ativo).HasColumnName("ativo");

        builder.HasIndex(n => n.Descricao);
    }
}
