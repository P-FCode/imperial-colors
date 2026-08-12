using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposIpiItemNotaFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_ipi",
                table: "itens_nota_fiscal",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "base_ipi",
                table: "itens_nota_fiscal",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_enquadramento_ipi",
                table: "itens_nota_fiscal",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aliquota_ipi",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "base_ipi",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "codigo_enquadramento_ipi",
                table: "itens_nota_fiscal");
        }
    }
}
