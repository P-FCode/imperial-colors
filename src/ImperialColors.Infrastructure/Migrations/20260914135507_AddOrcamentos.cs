using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrcamentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orcamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    numero_orcamento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    cliente_id = table.Column<int>(type: "integer", nullable: true),
                    nome_cliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefone_cliente = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    data_orcamento = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    data_validade = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    desconto = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    observacoes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orcamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_orcamentos_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "itens_orcamento",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    orcamento_id = table.Column<int>(type: "integer", nullable: false),
                    produto_id = table.Column<int>(type: "integer", nullable: true),
                    nome_produto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    codigo_produto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unidade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    quantidade = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    preco_unitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_orcamento", x => x.id);
                    table.ForeignKey(
                        name: "FK_itens_orcamento_orcamentos_orcamento_id",
                        column: x => x.orcamento_id,
                        principalTable: "orcamentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_itens_orcamento_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_itens_orcamento_orcamento_id",
                table: "itens_orcamento",
                column: "orcamento_id");

            migrationBuilder.CreateIndex(
                name: "IX_itens_orcamento_produto_id",
                table: "itens_orcamento",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_cliente_id",
                table: "orcamentos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_data_orcamento",
                table: "orcamentos",
                column: "data_orcamento");

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_numero_orcamento",
                table: "orcamentos",
                column: "numero_orcamento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orcamentos_status",
                table: "orcamentos",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "itens_orcamento");

            migrationBuilder.DropTable(
                name: "orcamentos");
        }
    }
}
