using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProdutoValidadeFornecedorPromocao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "data_validade",
                table: "produtos",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "fornecedor_id",
                table: "produtos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "preco_promocional",
                table: "produtos",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "promocao_ativa",
                table: "produtos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_produtos_fornecedor_id",
                table: "produtos",
                column: "fornecedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_promocao_ativa",
                table: "produtos",
                column: "promocao_ativa");

            migrationBuilder.AddForeignKey(
                name: "FK_produtos_fornecedores_fornecedor_id",
                table: "produtos",
                column: "fornecedor_id",
                principalTable: "fornecedores",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_produtos_fornecedores_fornecedor_id",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "IX_produtos_fornecedor_id",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "IX_produtos_promocao_ativa",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "data_validade",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "fornecedor_id",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "preco_promocional",
                table: "produtos");

            migrationBuilder.DropColumn(
                name: "promocao_ativa",
                table: "produtos");
        }
    }
}
