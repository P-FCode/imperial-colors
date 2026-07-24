using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLogsAuditoriaAndContingenciaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "contingencia_id",
                table: "vendas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "logs_auditoria",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    data_hora = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    nome_usuario = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    modulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    acao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    nivel = table.Column<int>(type: "integer", nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_auditoria", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vendas_contingencia_id",
                table: "vendas",
                column: "contingencia_id",
                unique: true,
                filter: "contingencia_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_logs_auditoria_data_hora",
                table: "logs_auditoria",
                column: "data_hora");

            migrationBuilder.CreateIndex(
                name: "IX_logs_auditoria_filtros",
                table: "logs_auditoria",
                columns: new[] { "data_hora", "nivel", "modulo" });

            migrationBuilder.CreateIndex(
                name: "IX_logs_auditoria_modulo",
                table: "logs_auditoria",
                column: "modulo");

            migrationBuilder.CreateIndex(
                name: "IX_logs_auditoria_nivel",
                table: "logs_auditoria",
                column: "nivel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logs_auditoria");

            migrationBuilder.DropIndex(
                name: "IX_vendas_contingencia_id",
                table: "vendas");

            migrationBuilder.DropColumn(
                name: "contingencia_id",
                table: "vendas");
        }
    }
}
