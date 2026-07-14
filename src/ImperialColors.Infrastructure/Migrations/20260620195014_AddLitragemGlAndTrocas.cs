using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLitragemGlAndTrocas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "litragem_gl",
                table: "produtos",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "trocas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    venda_origem_id = table.Column<int>(type: "integer", nullable: false),
                    produto_devolvido_id = table.Column<int>(type: "integer", nullable: false),
                    quantidade_devolvida = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    valor_unitario_devolucao = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    retornar_ao_estoque = table.Column<bool>(type: "boolean", nullable: false),
                    produto_novo_id = table.Column<int>(type: "integer", nullable: false),
                    quantidade_nova = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    valor_unitario_novo = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    forma_pagamento_diferenca = table.Column<int>(type: "integer", nullable: true),
                    observacoes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    usuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_troca = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trocas", x => x.id);
                    table.ForeignKey(
                        name: "FK_trocas_produtos_produto_devolvido_id",
                        column: x => x.produto_devolvido_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trocas_produtos_produto_novo_id",
                        column: x => x.produto_novo_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_trocas_vendas_venda_origem_id",
                        column: x => x.venda_origem_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_trocas_produto_devolvido_id",
                table: "trocas",
                column: "produto_devolvido_id");

            migrationBuilder.CreateIndex(
                name: "IX_trocas_produto_novo_id",
                table: "trocas",
                column: "produto_novo_id");

            migrationBuilder.CreateIndex(
                name: "IX_trocas_venda_origem_id",
                table: "trocas",
                column: "venda_origem_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trocas");

            migrationBuilder.DropColumn(
                name: "litragem_gl",
                table: "produtos");
        }
    }
}
