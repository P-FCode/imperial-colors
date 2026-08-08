using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCfopEConfiguracaoFiscalEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cfop_dentro_estado",
                table: "tributacao_produtos",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cfop_fora_estado",
                table: "tributacao_produtos",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cfop_dentro_estado",
                table: "tributacao_categorias",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cfop_fora_estado",
                table: "tributacao_categorias",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "configuracao_fiscal_empresa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    cep = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    complemento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    codigo_municipio_ibge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    nome_municipio = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    uf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    serie = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ambiente = table.Column<int>(type: "integer", nullable: false),
                    id_csc_homologacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    csc_homologacao = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    id_csc_producao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    csc_producao = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    simples_excesso_sublimite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    aliquota_ibs_uf_padrao = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    aliquota_ibs_municipio_padrao = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    aliquota_cbs_padrao = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracao_fiscal_empresa", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "cfop_dentro_estado",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "cfop_fora_estado",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "cfop_dentro_estado",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "cfop_fora_estado",
                table: "tributacao_categorias");
        }
    }
}
