using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDescricaoItemManualListaCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_itens_lista_compra_produtos_produto_id",
                table: "itens_lista_compra");

            migrationBuilder.AlterColumn<int>(
                name: "produto_id",
                table: "itens_lista_compra",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "descricao_item",
                table: "itens_lista_compra",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_itens_lista_compra_produtos_produto_id",
                table: "itens_lista_compra",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_itens_lista_compra_produtos_produto_id",
                table: "itens_lista_compra");

            migrationBuilder.DropColumn(
                name: "descricao_item",
                table: "itens_lista_compra");

            migrationBuilder.AlterColumn<int>(
                name: "produto_id",
                table: "itens_lista_compra",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_itens_lista_compra_produtos_produto_id",
                table: "itens_lista_compra",
                column: "produto_id",
                principalTable: "produtos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
