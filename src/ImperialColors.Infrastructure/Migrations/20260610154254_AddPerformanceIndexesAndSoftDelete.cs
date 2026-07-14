using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexesAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_vendas_data_venda",
                table: "vendas",
                column: "data_venda");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_nome",
                table: "produtos",
                column: "nome");

            migrationBuilder.CreateIndex(
                name: "IX_movimentacoes_estoque_criado_em",
                table: "movimentacoes_estoque",
                column: "criado_em");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vendas_data_venda",
                table: "vendas");

            migrationBuilder.DropIndex(
                name: "IX_produtos_nome",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "IX_movimentacoes_estoque_criado_em",
                table: "movimentacoes_estoque");
        }
    }
}
