using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTributacaoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tributacao_produtos",
                columns: table => new
                {
                    produto_id = table.Column<int>(type: "integer", nullable: false),
                    ncm = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    cest = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    origem = table.Column<int>(type: "integer", nullable: true),
                    cst_icms = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    csosn_icms = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    aliquota_icms = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    aliquota_icms_st = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    mva = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    reducao_base_calculo = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cst_pis = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    aliquota_pis = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cst_cofins = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    aliquota_cofins = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    cst_ipi = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    codigo_enquadramento_ipi = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    aliquota_ipi = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    unidade_tributavel = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    fator_conversao = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    gtin_tributavel = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tributacao_produtos", x => x.produto_id);
                    table.ForeignKey(
                        name: "FK_tributacao_produtos_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tributacao_produtos_ncm",
                table: "tributacao_produtos",
                column: "ncm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tributacao_produtos");
        }
    }
}
