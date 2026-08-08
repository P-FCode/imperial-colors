using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNaturezaOperacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_ibs_municipio_diferimento",
                table: "tributacao_produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_ibs_municipio_reducao",
                table: "tributacao_produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_is",
                table: "tributacao_produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ex_tipi",
                table: "tributacao_produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_ipi_fixo",
                table: "tributacao_produtos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_ibs_municipio_diferimento",
                table: "tributacao_categorias",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_ibs_municipio_reducao",
                table: "tributacao_categorias",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_is",
                table: "tributacao_categorias",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "bloquear_edicao_numero_nota",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "bloquear_nota_itens_menor_venda",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "c_class_trib_padrao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "cancelar_nota_automatico_venda",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "cnae",
                table: "configuracao_fiscal_empresa",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_ibs_cbs_padrao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "difal_nao_contribuinte",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "difal_st_contribuinte",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "email_padrao_envio_notas",
                table: "configuracao_fiscal_empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "frete_por_conta_padrao",
                table: "configuracao_fiscal_empresa",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "gerar_nota_automatica_venda",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ie_isenta",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "indicador_presenca_padrao",
                table: "configuracao_fiscal_empresa",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "inscricao_municipal",
                table: "configuracao_fiscal_empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inscricao_suframa",
                table: "configuracao_fiscal_empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "validar_ncm_em_notas",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "inscricoes_estaduais_substituto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    configuracao_fiscal_empresa_id = table.Column<int>(type: "integer", nullable: false),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    inscricao_estadual = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inscricoes_estaduais_substituto", x => x.id);
                    table.ForeignKey(
                        name: "FK_inscricoes_estaduais_substituto_configuracao_fiscal_empresa~",
                        column: x => x.configuracao_fiscal_empresa_id,
                        principalTable: "configuracao_fiscal_empresa",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "naturezas_operacao",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_operacao = table.Column<int>(type: "integer", nullable: false),
                    finalidade = table.Column<int>(type: "integer", nullable: false),
                    consumidor_final = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    serie = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    csosn_padrao = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    cst_icms_padrao = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cfop_dentro_estado = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    cfop_fora_estado = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    difal_nao_contribuinte = table.Column<bool>(type: "boolean", nullable: true),
                    observacoes_padrao = table.Column<string>(type: "text", nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_naturezas_operacao", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inscricoes_estaduais_substituto_configuracao_fiscal_empresa~",
                table: "inscricoes_estaduais_substituto",
                columns: new[] { "configuracao_fiscal_empresa_id", "uf" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_naturezas_operacao_descricao",
                table: "naturezas_operacao",
                column: "descricao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inscricoes_estaduais_substituto");

            migrationBuilder.DropTable(
                name: "naturezas_operacao");

            migrationBuilder.DropColumn(
                name: "aliquota_ibs_municipio_diferimento",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "aliquota_ibs_municipio_reducao",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "aliquota_is",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "ex_tipi",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "valor_ipi_fixo",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "aliquota_ibs_municipio_diferimento",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "aliquota_ibs_municipio_reducao",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "aliquota_is",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "bloquear_edicao_numero_nota",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "bloquear_nota_itens_menor_venda",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "c_class_trib_padrao",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "cancelar_nota_automatico_venda",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "cnae",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "cst_ibs_cbs_padrao",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "difal_nao_contribuinte",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "difal_st_contribuinte",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "email_padrao_envio_notas",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "frete_por_conta_padrao",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "gerar_nota_automatica_venda",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "ie_isenta",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "indicador_presenca_padrao",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "inscricao_municipal",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "inscricao_suframa",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "validar_ncm_em_notas",
                table: "configuracao_fiscal_empresa");
        }
    }
}
