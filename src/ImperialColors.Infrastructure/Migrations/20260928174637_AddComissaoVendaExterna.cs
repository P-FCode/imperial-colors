using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComissaoVendaExterna : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "comissao",
                table: "vendas_externas",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "comissao_paga",
                table: "vendas_externas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "comissao_paga_em",
                table: "vendas_externas",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "comissao",
                table: "vendas_externas");

            migrationBuilder.DropColumn(
                name: "comissao_paga",
                table: "vendas_externas");

            migrationBuilder.DropColumn(
                name: "comissao_paga_em",
                table: "vendas_externas");
        }
    }
}
