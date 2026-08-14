using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Alarga para <c>text</c> as três colunas que passaram a guardar segredo cifrado pela
    /// DPAPI (ver <c>Infrastructure.Security.ProtecaoSegredoFiscal</c>): o texto cifrado em
    /// base64 é várias vezes maior que o original — um CSC de 36 caracteres passa de 300 e não
    /// caberia nos <c>varchar(64)</c>/<c>varchar(200)</c> anteriores.
    ///
    /// O EF avisa "may result in the loss of data" ao gerar esta migration; é falso positivo:
    /// <c>varchar(n) → text</c> é alargamento, o Postgres não trunca nada.
    ///
    /// Valores legados em texto puro NÃO são convertidos aqui — a DPAPI é uma API do Windows,
    /// não existe do lado do Postgres para um <c>UPDATE</c> fazer isso. Eles continuam
    /// legíveis (o conversor identifica a ausência do prefixo <c>dpapi:v1:</c> e devolve o
    /// valor como está) e passam a ser gravados cifrados na primeira vez que a configuração
    /// fiscal for salva. Na prática isso se resolve sozinho na implantação, quando a API Key
    /// de produção e o CSC da SEFAZ são cadastrados pela primeira vez naquela máquina.
    /// </summary>
    public partial class ProtegerSegredosFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "csc_producao",
                table: "configuracao_fiscal_empresa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "csc_homologacao",
                table: "configuracao_fiscal_empresa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "api_key_fiscal",
                table: "configuracao_fiscal_empresa",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "csc_producao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "csc_homologacao",
                table: "configuracao_fiscal_empresa",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "api_key_fiscal",
                table: "configuracao_fiscal_empresa",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
