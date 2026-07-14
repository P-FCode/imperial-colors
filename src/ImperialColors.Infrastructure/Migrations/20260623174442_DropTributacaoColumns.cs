using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTributacaoColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aliquota_cofins",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "aliquota_pis",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "cest",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "codigo_enquadramento_ipi",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "csosn",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "cst_cofins",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "cst_icms",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "cst_ipi",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "cst_pis",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "ncm",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "origem",
                table: "produtos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_cofins",
                table: "produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_pis",
                table: "produtos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cest",
                table: "produtos",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_enquadramento_ipi",
                table: "produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "csosn",
                table: "produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_cofins",
                table: "produtos",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_icms",
                table: "produtos",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_ipi",
                table: "produtos",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_pis",
                table: "produtos",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ncm",
                table: "produtos",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "origem",
                table: "produtos",
                type: "integer",
                nullable: true);
        }
    }
}
