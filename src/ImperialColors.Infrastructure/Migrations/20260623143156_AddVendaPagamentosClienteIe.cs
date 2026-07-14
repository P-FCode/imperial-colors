using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendaPagamentosClienteIe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "documento_comprador_cupom",
                table: "vendas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nome_comprador_cupom",
                table: "vendas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_pessoa_comprador",
                table: "vendas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inscricao_estadual",
                table: "fornecedores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_pessoa",
                table: "fornecedores",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "cnpj",
                table: "clientes",
                type: "character varying(18)",
                maxLength: 18,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inscricao_estadual",
                table: "clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tipo_pessoa",
                table: "clientes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "venda_pagamentos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    venda_id = table.Column<int>(type: "integer", nullable: false),
                    forma_pagamento = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    valor_recebido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    atualizado_em = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venda_pagamentos", x => x.id);
                    table.ForeignKey(
                        name: "FK_venda_pagamentos_vendas_venda_id",
                        column: x => x.venda_id,
                        principalTable: "vendas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_venda_pagamentos_venda_id",
                table: "venda_pagamentos",
                column: "venda_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "venda_pagamentos");

            migrationBuilder.DropColumn(
                name: "documento_comprador_cupom",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "nome_comprador_cupom",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "tipo_pessoa_comprador",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "inscricao_estadual",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "tipo_pessoa",
                table: "fornecedores");

            migrationBuilder.DropColumn(
                name: "cnpj",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "inscricao_estadual",
                table: "clientes");

            migrationBuilder.DropColumn(
                name: "tipo_pessoa",
                table: "clientes");
        }
    }
}
