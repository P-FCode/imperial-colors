using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAliquotaIcmsPadraoEReducaoBaseItemNota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "reducao_base_calculo",
                table: "itens_nota_fiscal",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aliquota_icms_padrao",
                table: "configuracao_fiscal_empresa",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reducao_base_calculo",
                table: "itens_nota_fiscal");

            migrationBuilder.DropColumn(
                name: "aliquota_icms_padrao",
                table: "configuracao_fiscal_empresa");
        }
    }
}
