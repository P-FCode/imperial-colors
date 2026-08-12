using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmitenteFiscalRemoveIeSuframa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ie_isenta",
                table: "configuracao_fiscal_empresa");

            // Suframa e Inscrição Estadual são conceitos diferentes — mesmo com tipo/
            // tamanho de coluna iguais, um valor de Suframa já cadastrado não pode virar
            // Inscrição Estadual por acidente (o que RenameColumn faria).
            migrationBuilder.DropColumn(
                name: "inscricao_suframa",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.AddColumn<string>(
                name: "inscricao_estadual",
                table: "configuracao_fiscal_empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cnpj",
                table: "configuracao_fiscal_empresa",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nome_fantasia",
                table: "configuracao_fiscal_empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "razao_social",
                table: "configuracao_fiscal_empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cnpj",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "nome_fantasia",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "razao_social",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.DropColumn(
                name: "inscricao_estadual",
                table: "configuracao_fiscal_empresa");

            migrationBuilder.AddColumn<string>(
                name: "inscricao_suframa",
                table: "configuracao_fiscal_empresa",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ie_isenta",
                table: "configuracao_fiscal_empresa",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
