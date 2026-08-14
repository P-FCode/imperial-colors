using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Inclui <c>ambiente</c> na chave da numeração fiscal — tanto no índice único que protege
    /// a sequência quanto no índice de expressão que responde o <c>MAX(CAST(numero AS INTEGER))</c>
    /// de <c>NotaFiscalRepository.ObterProximoNumeroAsync</c>.
    ///
    /// Na SEFAZ, homologação (<c>tpAmb=2</c>) e produção (<c>tpAmb=1</c>) são sequências
    /// completamente independentes: a chave de acesso de 44 dígitos não tem sequer um dígito
    /// de ambiente (<c>cUF+AAMM+CNPJ+mod+serie+nNF+tpEmis+cNF+cDV</c>) — o ambiente viaja no
    /// XML. Tratar as duas como uma sequência só, como este banco fazia, faz cada nota de
    /// teste empurrar a numeração de produção para frente e vice-versa; como a numeração é
    /// imutável, o número pulado nunca volta e o buraco fica permanente nas duas séries.
    ///
    /// A troca do índice único é uma RELAXAÇÃO (a chave ganha uma coluna), então não falha por
    /// dado preexistente: o que era único continua único.
    /// </summary>
    public partial class NumeracaoFiscalPorAmbiente : Migration
    {
        private const string ExpressaoAntigo = "IX_notas_fiscais_tipo_serie_numero_int";
        private const string ExpressaoNovo = "IX_notas_fiscais_tipo_serie_ambiente_numero_int";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais");

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_tipo_serie_ambiente_numero",
                table: "notas_fiscais",
                columns: new[] { "tipo", "serie", "ambiente", "numero" },
                unique: true);

            // Índice de expressão (fora do modelo — o cast text→integer não é representável
            // por HasIndex), criado em AddIndiceNumeracaoNotaFiscal. Sem o ambiente como
            // terceira coluna, a consulta filtrada por ambiente deixaria de ser resolvida
            // pela primeira entrada do índice e voltaria a varrer a série inteira.
            migrationBuilder.Sql($"""
                DROP INDEX IF EXISTS "{ExpressaoAntigo}";
                CREATE INDEX IF NOT EXISTS "{ExpressaoNovo}"
                    ON notas_fiscais (tipo, serie, ambiente, ((numero)::integer) DESC)
                 WHERE numero ~ '^[0-9]+$';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DROP INDEX IF EXISTS "{ExpressaoNovo}";
                CREATE INDEX IF NOT EXISTS "{ExpressaoAntigo}"
                    ON notas_fiscais (tipo, serie, ((numero)::integer) DESC)
                 WHERE numero ~ '^[0-9]+$';
                """);

            migrationBuilder.DropIndex(
                name: "IX_notas_fiscais_tipo_serie_ambiente_numero",
                table: "notas_fiscais");

            // Pode falhar legitimamente: se já existir o mesmo (tipo, serie, numero) em
            // homologação e em produção — exatamente o que o Up passou a permitir — o índice
            // único antigo não tem como ser recriado, e a duplicata precisa ser resolvida à mão.
            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_tipo_serie_numero",
                table: "notas_fiscais",
                columns: new[] { "tipo", "serie", "numero" },
                unique: true);
        }
    }
}
