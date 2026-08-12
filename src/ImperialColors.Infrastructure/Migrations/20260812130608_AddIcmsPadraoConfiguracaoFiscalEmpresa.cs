using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIcmsPadraoConfiguracaoFiscalEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "csosn_icms_padrao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cst_icms_padrao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "csosn_icms_padrao",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "cst_icms_padrao",
                table: "configuracao_fiscal_empresa");
        }
    }
}
