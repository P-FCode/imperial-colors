using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    venda_id = table.Column<int>(type: "integer", nullable: true),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    natureza_operacao_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_saida = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    serie = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    numero = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    data_emissao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_saida = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    natureza_operacao_descricao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    finalidade = table.Column<int>(type: "integer", nullable: false),
                    consumidor_final = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    indicador_presenca = table.Column<int>(type: "integer", nullable: false),
                    intermediador_cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    intermediador_identificador = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    crt = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    ambiente = table.Column<int>(type: "integer", nullable: false),
                    destinatario_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    destinatario_tipo_pessoa = table.Column<int>(type: "integer", nullable: true),
                    destinatario_documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    destinatario_indicador_ie = table.Column<int>(type: "integer", nullable: true),
                    destinatario_inscricao_estadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    destinatario_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    destinatario_telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    destinatario_cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    destinatario_logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    destinatario_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    destinatario_complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    destinatario_bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    destinatario_cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    destinatario_uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    destinatario_codigo_municipio_ibge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    suframa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    vendedor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    lista_preco_nome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    entrega_diferente_cobranca = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    entrega_cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    entrega_logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    entrega_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    entrega_complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entrega_bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entrega_cidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entrega_uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    entrega_codigo_municipio_ibge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    calculo_automatico = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    v_prod = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_serv = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_frete = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_seg = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_bc_icms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_icms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_bc_icms_st = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_icms_st = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_ipi = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_ipi_devolvido = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_issqn = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_outro = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_desc = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_funrural = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    numero_itens = table.Column<int>(type: "integer", nullable: false),
                    v_aprox_imp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_fcp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_fcp_st = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_fcp_st_ret = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_bc_ibs_cbs = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_ibs_uf = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_ibs_municipio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_ibs = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_cbs = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    v_nf = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    forma_envio = table.Column<int>(type: "integer", nullable: false),
                    peso_bruto = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    peso_liquido = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    enviar_para_expedicao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    forma_recebimento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    categoria_financeira = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    condicao_pagamento = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deposito = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    observacoes_sistema = table.Column<string>(type: "text", nullable: true),
                    informacoes_fisco = table.Column<string>(type: "text", nullable: true),
                    marcadores = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    chave_acesso = table.Column<string>(type: "character varying(44)", maxLength: 44, nullable: true),
                    n_prot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    dh_recbto = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    c_stat = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    x_motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xml_autorizado = table.Column<string>(type: "text", nullable: true),
                    qr_code_url = table.Column<string>(type: "text", nullable: true),
                    caminho_xml_local = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mensagem_erro = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.id);
                    table.ForeignKey(
                        name: "FK_notas_fiscais_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notas_fiscais_naturezas_operacao_natureza_operacao_id",
                        column: x => x.natureza_operacao_id,
                        principalTable: "naturezas_operacao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_notas_fiscais_vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "itens_nota_fiscal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nota_fiscal_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: true),
                    n_item = table.Column<int>(type: "integer", nullable: false),
                    codigo_produto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    descricao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    cest = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    cfop = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    unidade = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unidade_tributavel = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    quantidade_tributavel = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: true),
                    valor_unitario_tributavel = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: true),
                    compoe_total_nota = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    origem = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    cst_icms = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    csosn_icms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    base_icms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_icms = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_icms = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    base_icms_st = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_icms_st = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_icms_st = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    cst_pis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    base_pis = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_pis = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_pis = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    cst_cofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    base_cofins = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_cofins = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_cofins = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    cst_ipi = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    valor_ipi = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    cst_ibs_cbs = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    c_class_trib = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    base_ibs_cbs = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_ibs_uf = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_ibs_uf = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_ibs_municipio = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_ibs_municipio = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    aliquota_cbs = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    valor_cbs = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_nota_fiscal", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_nota_fiscal_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itens_nota_fiscal_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "nota_fiscal_eventos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nota_fiscal_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    sucesso = table.Column<bool>(type: "boolean", nullable: false),
                    n_prot_evento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    c_stat = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    x_motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    texto = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sequencial = table.Column<int>(type: "integer", nullable: true),
                    trace_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nota_fiscal_eventos", x => x.id);
                    table.ForeignKey(
                        name: "FK_nota_fiscal_eventos_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nota_fiscal_pagamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    nota_fiscal_id = table.Column<int>(type: "integer", nullable: false),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nota_fiscal_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_nota_fiscal_pagamentos_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itens_nota_fiscal_nota_fiscal_id",
                table: "itens_nota_fiscal",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_nota_fiscal_produto_id",
                table: "itens_nota_fiscal",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_nota_fiscal_eventos_nota_fiscal_id",
                table: "nota_fiscal_eventos",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "IX_nota_fiscal_pagamentos_nota_fiscal_id",
                table: "nota_fiscal_pagamentos",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_chave_acesso",
                table: "notas_fiscais",
                column: "chave_acesso",
                unique: true,
                filter: "chave_acesso IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_cliente_id",
                table: "notas_fiscais",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_natureza_operacao_id",
                table: "notas_fiscais",
                column: "natureza_operacao_id");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_status",
                table: "notas_fiscais",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais",
                columns: new[] { "tipo", "serie", "numero" });

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_venda_id",
                table: "notas_fiscais",
                column: "venda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_nota_fiscal");

            migrationBuilder.DropTable(
                name: "nota_fiscal_eventos");

            migrationBuilder.DropTable(
                name: "nota_fiscal_pagamentos");

            migrationBuilder.DropTable(
                name: "notas_fiscais");
        }
    }
}
