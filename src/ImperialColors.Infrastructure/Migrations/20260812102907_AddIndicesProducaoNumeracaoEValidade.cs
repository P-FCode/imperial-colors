using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicesProducaoNumeracaoEValidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais");

            migrationBuilder.CreateIndex(
                name: "IX_produtos_data_validade",
                table: "produtos",
                column: "data_validade");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais",
                columns: new[] { "tipo", "serie", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_produtos_data_validade",
                table: "produtos");

            migrationBuilder.DropIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais",
                columns: new[] { "tipo", "serie", "numero" });
        }
    }
}
