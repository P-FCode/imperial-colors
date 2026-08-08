using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReformaTributariaIbsCbs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "c_class_trib",
                table: "tributacao_produtos",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "c_class_trib_is",
                table: "tributacao_produtos",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_ibs_cbs",
                table: "tributacao_produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_is",
                table: "tributacao_produtos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "c_class_trib",
                table: "tributacao_categorias",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "c_class_trib_is",
                table: "tributacao_categorias",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_ibs_cbs",
                table: "tributacao_categorias",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_is",
                table: "tributacao_categorias",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "c_class_trib",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "c_class_trib_is",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "cst_ibs_cbs",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "cst_is",
                table: "tributacao_produtos");

            migrationBuilder.DropColumn(
                name: "c_class_trib",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "c_class_trib_is",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "cst_ibs_cbs",
                table: "tributacao_categorias");

            migrationBuilder.DropColumn(
                name: "cst_is",
                table: "tributacao_categorias");
        }
    }
}
