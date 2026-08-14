using System.Runtime.Versioning;
using ImperialColors.Domain.Entities;
using ImperialColors.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ImperialColors.Infrastructure.Data.Mappings;

[SupportedOSPlatform("windows")]
public class ConfiguracaoFiscalEmpresaMapping : IEntityTypeConfiguration<ConfiguracaoFiscalEmpresa>
{
    /// <summary>Cifra ao gravar, decifra ao ler. Ver <see cref="ProtecaoSegredoFiscal"/>.</summary>
    private static readonly ValueConverter<string?, string?> ConversorSegredo = new(
        aoGravar => ProtecaoSegredoFiscal.Proteger(aoGravar),
        aoLer => ProtecaoSegredoFiscal.Desproteger(aoLer));


    public void Configure(EntityTypeBuilder<ConfiguracaoFiscalEmpresa> builder)
    {
        builder.ToTable("configuracao_fiscal_empresa");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(c => c.Cnpj).HasColumnName("cnpj").HasMaxLength(14);
        builder.Property(c => c.RazaoSocial).HasColumnName("razao_social").HasMaxLength(200);
        builder.Property(c => c.NomeFantasia).HasColumnName("nome_fantasia").HasMaxLength(200);
        builder.Property(c => c.InscricaoEstadual).HasColumnName("inscricao_estadual").HasMaxLength(20);
        builder.Property(c => c.InscricaoMunicipal).HasColumnName("inscricao_municipal").HasMaxLength(20);
        builder.Property(c => c.Cnae).HasColumnName("cnae").HasMaxLength(10);

        builder.Property(c => c.DifalNaoContribuinte).HasColumnName("difal_nao_contribuinte").HasDefaultValue(false);
        builder.Property(c => c.DifalStContribuinte).HasColumnName("difal_st_contribuinte").HasDefaultValue(false);

        builder.Property(c => c.Cep).HasColumnName("cep").HasMaxLength(8);
        builder.Property(c => c.Logradouro).HasColumnName("logradouro").HasMaxLength(200);
        builder.Property(c => c.Numero).HasColumnName("numero").HasMaxLength(20);
        builder.Property(c => c.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(c => c.Bairro).HasColumnName("bairro").HasMaxLength(100);
        builder.Property(c => c.CodigoMunicipioIbge).HasColumnName("codigo_municipio_ibge").HasMaxLength(7);
        builder.Property(c => c.NomeMunicipio).HasColumnName("nome_municipio").HasMaxLength(100);
        builder.Property(c => c.Uf).HasColumnName("uf").HasMaxLength(2);

        builder.Property(c => c.Serie).HasColumnName("serie").HasMaxLength(3);

        builder.Property(c => c.Ambiente).HasColumnName("ambiente");

        // Os três segredos abaixo (CSC de cada ambiente e API Key) passam por
        // ProtecaoSegredoFiscal na ida e na volta — o banco guarda só o texto cifrado pela
        // DPAPI. O ValueConverter aplica isso de forma transparente: nenhum repositório,
        // serviço ou tela precisa saber que existe criptografia no caminho.
        //
        // Sem HasMaxLength: o texto cifrado + base64 é várias vezes maior que o original
        // (um CSC de 36 caracteres passa de 300), então as colunas viraram `text` na
        // migration ProtegerSegredosFiscais. Os IDs de CSC não são segredo (são identificadores
        // públicos que acompanham o CSC) e continuam em claro.
        builder.Property(c => c.IdCscHomologacao).HasColumnName("id_csc_homologacao").HasMaxLength(20);
        builder.Property(c => c.CscHomologacao).HasColumnName("csc_homologacao")
            .HasConversion(ConversorSegredo);
        builder.Property(c => c.IdCscProducao).HasColumnName("id_csc_producao").HasMaxLength(20);
        builder.Property(c => c.CscProducao).HasColumnName("csc_producao")
            .HasConversion(ConversorSegredo);

        builder.Property(c => c.ApiKeyFiscal).HasColumnName("api_key_fiscal")
            .HasConversion(ConversorSegredo);

        builder.Property(c => c.SimplesExcessoSublimite).HasColumnName("simples_excesso_sublimite").HasDefaultValue(false);

        builder.Property(c => c.CstIcmsPadrao).HasColumnName("cst_icms_padrao").HasMaxLength(2);
        builder.Property(c => c.CsosnIcmsPadrao).HasColumnName("csosn_icms_padrao").HasMaxLength(3);
        builder.Property(c => c.AliquotaIcmsPadrao).HasColumnName("aliquota_icms_padrao").HasPrecision(5, 2);

        builder.Property(c => c.AliquotaIbsUfPadrao).HasColumnName("aliquota_ibs_uf_padrao").HasPrecision(6, 4);
        builder.Property(c => c.AliquotaIbsMunicipioPadrao).HasColumnName("aliquota_ibs_municipio_padrao").HasPrecision(6, 4);
        builder.Property(c => c.AliquotaCbsPadrao).HasColumnName("aliquota_cbs_padrao").HasPrecision(6, 4);

        builder.Property(c => c.CstIbsCbsPadrao).HasColumnName("cst_ibs_cbs_padrao").HasMaxLength(3);
        builder.Property(c => c.CClassTribPadrao).HasColumnName("c_class_trib_padrao").HasMaxLength(6);

        builder.Property(c => c.ValidarNcmEmNotas).HasColumnName("validar_ncm_em_notas").HasDefaultValue(true);
        builder.Property(c => c.BloquearEdicaoNumeroNota).HasColumnName("bloquear_edicao_numero_nota").HasDefaultValue(false);
        builder.Property(c => c.BloquearNotaComItensMenorQueVenda).HasColumnName("bloquear_nota_itens_menor_venda").HasDefaultValue(true);
        builder.Property(c => c.FretePorContaPadrao).HasColumnName("frete_por_conta_padrao");
        builder.Property(c => c.EmailPadraoEnvioNotas).HasColumnName("email_padrao_envio_notas").HasMaxLength(200);
        builder.Property(c => c.IndicadorPresencaPadrao).HasColumnName("indicador_presenca_padrao");
        builder.Property(c => c.GerarNotaAutomaticaAoFinalizarVenda).HasColumnName("gerar_nota_automatica_venda").HasDefaultValue(false);
        builder.Property(c => c.CancelarNotaAutomaticoAoCancelarVenda).HasColumnName("cancelar_nota_automatico_venda").HasDefaultValue(false);

        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");

        builder.HasMany(c => c.InscricoesSubstitutoTributario)
            .WithOne(i => i.ConfiguracaoFiscalEmpresa)
            .HasForeignKey(i => i.ConfiguracaoFiscalEmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
