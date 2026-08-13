using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcmsStParaFrenteEStRetido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_icms_st_retido",
                table: "tributacao_produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_icms_st_retido",
                table: "tributacao_categorias",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_icms_st_retido",
                table: "itens_nota_fiscal",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_icms_st_retido",
                table: "itens_nota_fiscal",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "mva",
                table: "itens_nota_fiscal",
                type: "numeric(7,4)",
                precision: 7,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_icms_st_retido",
                table: "itens_nota_fiscal",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aliquota_icms_st_retido",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "aliquota_icms_st_retido",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "aliquota_icms_st_retido",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "base_icms_st_retido",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "mva",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "valor_icms_st_retido",
                table: "itens_nota_fiscal");
        }
    }
}
