using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendasExternas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "venda_externa_id",
                table: "movimentacoes_estoque",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vendas_externas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    numero_venda_externa = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_venda = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendas_externas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itens_venda_externa",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    venda_externa_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: true),
                    nome_produto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    codigo_barras = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantidade = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    preco_base = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_venda_externa", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_venda_externa_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_itens_venda_externa_vendas_externas_venda_externa_id",
                        column: x => x.venda_externa_id,
                        principalTable: "vendas_externas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimentacoes_estoque_venda_externa_id",
                table: "movimentacoes_estoque",
                column: "venda_externa_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_venda_externa_produto_id",
                table: "itens_venda_externa",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_venda_externa_venda_externa_id",
                table: "itens_venda_externa",
                column: "venda_externa_id");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_externas_data_venda",
                table: "vendas_externas",
                column: "data_venda");

            migrationBuilder.CreateIndex(
                name: "IX_vendas_externas_numero_venda_externa",
                table: "vendas_externas",
                column: "numero_venda_externa",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_movimentacoes_estoque_vendas_externas_venda_externa_id",
                table: "movimentacoes_estoque",
                column: "venda_externa_id",
                principalTable: "vendas_externas",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_movimentacoes_estoque_vendas_externas_venda_externa_id",
                table: "movimentacoes_estoque");

            migrationBuilder.DropTable(
                name: "itens_venda_externa");

            migrationBuilder.DropTable(
                name: "vendas_externas");

            migrationBuilder.DropIndex(
                name: "IX_movimentacoes_estoque_venda_externa_id",
                table: "movimentacoes_estoque");

            migrationBuilder.DropColumn(
                name: "venda_externa_id",
                table: "movimentacoes_estoque");
        }
    }
}
