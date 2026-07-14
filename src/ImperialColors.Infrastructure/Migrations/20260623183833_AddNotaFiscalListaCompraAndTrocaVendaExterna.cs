using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaFiscalListaCompraAndTrocaVendaExterna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "venda_origem_id",
                table: "trocas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "venda_externa_origem_id",
                table: "trocas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "nota_fiscal_conteudo",
                table: "listas_compra",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nota_fiscal_nome_arquivo",
                table: "listas_compra",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_trocas_venda_externa_origem_id",
                table: "trocas",
                column: "venda_externa_origem_id");

            migrationBuilder.AddForeignKey(
                name: "FK_trocas_vendas_externas_venda_externa_origem_id",
                table: "trocas",
                column: "venda_externa_origem_id",
                principalTable: "vendas_externas",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_trocas_vendas_externas_venda_externa_origem_id",
                table: "trocas");

            migrationBuilder.DropIndex(
                name: "IX_trocas_venda_externa_origem_id",
                table: "trocas");

            migrationBuilder.DropColumn(
                name: "venda_externa_origem_id",
                table: "trocas");

            migrationBuilder.DropColumn(
                name: "nota_fiscal_conteudo",
                table: "listas_compra");

            migrationBuilder.DropColumn(
                name: "nota_fiscal_nome_arquivo",
                table: "listas_compra");

            migrationBuilder.AlterColumn<int>(
                name: "venda_origem_id",
                table: "trocas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
