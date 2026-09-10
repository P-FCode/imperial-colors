using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Substitui <c>litragem_gl</c> (decimal, só para Galão, valores fixos 3,6/18) por
    /// <c>tamanho_embalagem</c> (texto livre, qualquer unidade — "18L", "3,6L", "25 KG").
    /// A importação do catálogo Paraná trouxe embalagens em kg (balde) que um campo
    /// decimal batizado de "litragem" nunca poderia representar sem mentir.
    ///
    /// O EF avisa "pode resultar em perda de dados" ao gerar esta migration; é esperado e
    /// inofensivo — não há produto real usando este campo ainda (só residual de testes de
    /// integração antigos), então não há nada de valor para migrar de um tipo para o outro.
    /// </summary>
    public partial class RenomearLitragemGlParaTamanhoEmbalagem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "litragem_gl",
                table: "produtos");

            migrationBuilder.AddColumn<string>(
                name: "tamanho_embalagem",
                table: "produtos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tamanho_embalagem",
                table: "produtos");

            migrationBuilder.AddColumn<decimal>(
                name: "litragem_gl",
                table: "produtos",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);
        }
    }
}
