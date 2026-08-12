using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class NotaFiscalMapping : IEntityTypeConfiguration<NotaFiscal>
{
    public void Configure(EntityTypeBuilder<NotaFiscal> builder)
    {
        builder.ToTable("notas_fiscais");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.Property(n => n.Tipo).HasColumnName("tipo").IsRequired();
        builder.Property(n => n.VendaId).HasColumnName("venda_id");
        builder.Property(n => n.ClienteId).HasColumnName("cliente_id");
        builder.Property(n => n.NaturezaOperacaoId).HasColumnName("natureza_operacao_id");
        builder.Property(n => n.TipoSaida).HasColumnName("tipo_saida").HasMaxLength(60);
        builder.Property(n => n.Serie).HasColumnName("serie").HasMaxLength(3).IsRequired();
        builder.Property(n => n.Numero).HasColumnName("numero").HasMaxLength(9).IsRequired();
        builder.Property(n => n.DataEmissao).HasColumnName("data_emissao");
        builder.Property(n => n.DataSaida).HasColumnName("data_saida");
        builder.Property(n => n.NaturezaOperacaoDescricao).HasColumnName("natureza_operacao_descricao").HasMaxLength(60);
        builder.Property(n => n.Finalidade).HasColumnName("finalidade");
        builder.Property(n => n.ConsumidorFinal).HasColumnName("consumidor_final").HasDefaultValue(true);
        builder.Property(n => n.IndicadorPresenca).HasColumnName("indicador_presenca");
        builder.Property(n => n.IntermediadorCnpj).HasColumnName("intermediador_cnpj").HasMaxLength(14);
        builder.Property(n => n.IntermediadorIdentificador).HasColumnName("intermediador_identificador").HasMaxLength(60);
        builder.Property(n => n.Crt).HasColumnName("crt").HasMaxLength(1);
        builder.Property(n => n.Ambiente).HasColumnName("ambiente");

        builder.Property(n => n.DestinatarioNome).HasColumnName("destinatario_nome").HasMaxLength(200);
        builder.Property(n => n.DestinatarioTipoPessoa).HasColumnName("destinatario_tipo_pessoa");
        builder.Property(n => n.DestinatarioDocumento).HasColumnName("destinatario_documento").HasMaxLength(14);
        builder.Property(n => n.DestinatarioIndicadorIe).HasColumnName("destinatario_indicador_ie");
        builder.Property(n => n.DestinatarioInscricaoEstadual).HasColumnName("destinatario_inscricao_estadual").HasMaxLength(20);
        builder.Property(n => n.DestinatarioEmail).HasColumnName("destinatario_email").HasMaxLength(200);
        builder.Property(n => n.DestinatarioTelefone).HasColumnName("destinatario_telefone").HasMaxLength(20);
        builder.Property(n => n.DestinatarioCep).HasColumnName("destinatario_cep").HasMaxLength(8);
        builder.Property(n => n.DestinatarioLogradouro).HasColumnName("destinatario_logradouro").HasMaxLength(200);
        builder.Property(n => n.DestinatarioNumero).HasColumnName("destinatario_numero").HasMaxLength(20);
        builder.Property(n => n.DestinatarioComplemento).HasColumnName("destinatario_complemento").HasMaxLength(100);
        builder.Property(n => n.DestinatarioBairro).HasColumnName("destinatario_bairro").HasMaxLength(100);
        builder.Property(n => n.DestinatarioCidade).HasColumnName("destinatario_cidade").HasMaxLength(100);
        builder.Property(n => n.DestinatarioUf).HasColumnName("destinatario_uf").HasMaxLength(2);
        builder.Property(n => n.DestinatarioCodigoMunicipioIbge).HasColumnName("destinatario_codigo_municipio_ibge").HasMaxLength(7);
        builder.Property(n => n.Suframa).HasColumnName("suframa").HasMaxLength(20);
        builder.Property(n => n.Vendedor).HasColumnName("vendedor").HasMaxLength(100);
        builder.Property(n => n.ListaPrecoNome).HasColumnName("lista_preco_nome").HasMaxLength(60);

        builder.Property(n => n.EntregaDiferenteCobranca).HasColumnName("entrega_diferente_cobranca").HasDefaultValue(false);
        builder.Property(n => n.EntregaCep).HasColumnName("entrega_cep").HasMaxLength(8);
        builder.Property(n => n.EntregaLogradouro).HasColumnName("entrega_logradouro").HasMaxLength(200);
        builder.Property(n => n.EntregaNumero).HasColumnName("entrega_numero").HasMaxLength(20);
        builder.Property(n => n.EntregaComplemento).HasColumnName("entrega_complemento").HasMaxLength(100);
        builder.Property(n => n.EntregaBairro).HasColumnName("entrega_bairro").HasMaxLength(100);
        builder.Property(n => n.EntregaCidade).HasColumnName("entrega_cidade").HasMaxLength(100);
        builder.Property(n => n.EntregaUf).HasColumnName("entrega_uf").HasMaxLength(2);
        builder.Property(n => n.EntregaCodigoMunicipioIbge).HasColumnName("entrega_codigo_municipio_ibge").HasMaxLength(7);

        builder.Property(n => n.CalculoAutomatico).HasColumnName("calculo_automatico").HasDefaultValue(true);
        builder.Property(n => n.VProd).HasColumnName("v_prod").HasPrecision(12, 2);
        builder.Property(n => n.VServ).HasColumnName("v_serv").HasPrecision(12, 2);
        builder.Property(n => n.VFrete).HasColumnName("v_frete").HasPrecision(12, 2);
        builder.Property(n => n.VSeg).HasColumnName("v_seg").HasPrecision(12, 2);
        builder.Property(n => n.VBcIcms).HasColumnName("v_bc_icms").HasPrecision(12, 2);
        builder.Property(n => n.VIcms).HasColumnName("v_icms").HasPrecision(12, 2);
        builder.Property(n => n.VBcIcmsSt).HasColumnName("v_bc_icms_st").HasPrecision(12, 2);
        builder.Property(n => n.VIcmsSt).HasColumnName("v_icms_st").HasPrecision(12, 2);
        builder.Property(n => n.VIpi).HasColumnName("v_ipi").HasPrecision(12, 2);
        builder.Property(n => n.VIpiDevolvido).HasColumnName("v_ipi_devolvido").HasPrecision(12, 2);
        builder.Property(n => n.VIssqn).HasColumnName("v_issqn").HasPrecision(12, 2);
        builder.Property(n => n.VOutro).HasColumnName("v_outro").HasPrecision(12, 2);
        builder.Property(n => n.VDesc).HasColumnName("v_desc").HasPrecision(12, 2);
        builder.Property(n => n.VFunrural).HasColumnName("v_funrural").HasPrecision(12, 2);
        builder.Property(n => n.VAproxImp).HasColumnName("v_aprox_imp").HasPrecision(12, 2);
        builder.Property(n => n.VFcp).HasColumnName("v_fcp").HasPrecision(12, 2);
        builder.Property(n => n.VFcpSt).HasColumnName("v_fcp_st").HasPrecision(12, 2);
        builder.Property(n => n.VFcpStRet).HasColumnName("v_fcp_st_ret").HasPrecision(12, 2);
        builder.Property(n => n.VBcIbsCbs).HasColumnName("v_bc_ibs_cbs").HasPrecision(12, 2);
        builder.Property(n => n.VIbsUf).HasColumnName("v_ibs_uf").HasPrecision(12, 2);
        builder.Property(n => n.VIbsMunicipio).HasColumnName("v_ibs_municipio").HasPrecision(12, 2);
        builder.Property(n => n.VIbs).HasColumnName("v_ibs").HasPrecision(12, 2);
        builder.Property(n => n.VCbs).HasColumnName("v_cbs").HasPrecision(12, 2);
        builder.Property(n => n.VNf).HasColumnName("v_nf").HasPrecision(12, 2);
        builder.Property(n => n.NumeroItens).HasColumnName("numero_itens");

        builder.Property(n => n.FormaEnvio).HasColumnName("forma_envio");
        builder.Property(n => n.PesoBruto).HasColumnName("peso_bruto").HasPrecision(10, 3);
        builder.Property(n => n.PesoLiquido).HasColumnName("peso_liquido").HasPrecision(10, 3);
        builder.Property(n => n.EnviarParaExpedicao).HasColumnName("enviar_para_expedicao").HasDefaultValue(false);

        builder.Property(n => n.FormaRecebimento).HasColumnName("forma_recebimento").HasMaxLength(60);
        builder.Property(n => n.CategoriaFinanceira).HasColumnName("categoria_financeira").HasMaxLength(60);
        builder.Property(n => n.CondicaoPagamento).HasColumnName("condicao_pagamento").HasMaxLength(200);

        builder.Property(n => n.Deposito).HasColumnName("deposito").HasMaxLength(60);
        builder.Property(n => n.Observacoes).HasColumnName("observacoes");
        builder.Property(n => n.ObservacoesSistema).HasColumnName("observacoes_sistema");
        builder.Property(n => n.InformacoesFisco).HasColumnName("informacoes_fisco");
        builder.Property(n => n.Marcadores).HasColumnName("marcadores").HasMaxLength(300);

        builder.Property(n => n.Status).HasColumnName("status").HasDefaultValue(Domain.Enums.StatusNotaFiscal.Rascunho);
        builder.Property(n => n.ChaveAcesso).HasColumnName("chave_acesso").HasMaxLength(44);
        builder.Property(n => n.NProt).HasColumnName("n_prot").HasMaxLength(20);
        builder.Property(n => n.DhRecbto).HasColumnName("dh_recbto");
        builder.Property(n => n.CStat).HasColumnName("c_stat").HasMaxLength(5);
        builder.Property(n => n.XMotivo).HasColumnName("x_motivo").HasMaxLength(500);
        builder.Property(n => n.XmlAutorizado).HasColumnName("xml_autorizado").HasColumnType("text");
        builder.Property(n => n.QrCodeUrl).HasColumnName("qr_code_url").HasColumnType("text");
        builder.Property(n => n.CaminhoXmlLocal).HasColumnName("caminho_xml_local").HasMaxLength(500);
        builder.Property(n => n.TraceId).HasColumnName("trace_id").HasMaxLength(100);
        builder.Property(n => n.MensagemErro).HasColumnName("mensagem_erro").HasMaxLength(1000);

        builder.Property(n => n.CriadoEm).HasColumnName("criado_em");
        builder.Property(n => n.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(n => n.Ativo).HasColumnName("ativo");

        builder.HasIndex(n => n.ChaveAcesso).IsUnique().HasFilter("chave_acesso IS NOT NULL");
        // Único (não só índice de busca) — rede de segurança de integridade contra a corrida
        // entre "consultar o próximo número" (ObterProximoNumeroAsync) e "salvar a nota"
        // (CriarAsync), que hoje não é protegida por lock. Sem essa constraint, duas emissões
        // concorrentes podiam gerar silenciosamente o mesmo (Tipo, Serie, Numero) — a
        // numeração é imutável mesmo para notas rejeitadas/soft-deletadas (ver
        // ObterProximoNumeroAsync, que ignora o filtro de soft-delete de propósito), então
        // não há cenário legítimo de reaproveitar um número já usado.
        builder.HasIndex(n => new { n.Tipo, n.Serie, n.Numero }).IsUnique().HasDatabaseName("IX_notas_fiscais_tipo_serie_numero");
        builder.HasIndex(n => n.Status);

        builder.HasOne(n => n.Venda).WithMany().HasForeignKey(n => n.VendaId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.Cliente).WithMany().HasForeignKey(n => n.ClienteId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.NaturezaOperacao).WithMany().HasForeignKey(n => n.NaturezaOperacaoId).OnDelete(DeleteBehavior.SetNull);
    }
}
