using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Índice de expressão que atende o <c>MAX(CAST(numero AS INTEGER))</c> de
    /// <c>NotaFiscalRepository.ObterProximoNumeroAsync</c>, chamado a cada rascunho criado e
    /// a cada reemissão.
    ///
    /// O índice único já existente (<c>IX_notas_fiscais_tipo_serie_numero</c>) não serve para
    /// isso: ele ordena <c>numero</c> como TEXTO, e como a coluna é <c>varchar(9)</c> sem zero
    /// à esquerda a ordem lexicográfica diverge da numérica ("9" &gt; "12" &gt; "100"). Buscar o
    /// maior número por ele daria a resposta errada, e é por isso que o código antigo trazia
    /// a série inteira para a memória.
    ///
    /// Com <c>(numero)::integer DESC</c> na terceira posição, o Postgres resolve o MAX lendo
    /// a primeira entrada do índice para aquele (tipo, série) — custo constante, independente
    /// de a série ter 10 ou 10 milhões de notas. O cast text→integer é IMMUTABLE, requisito
    /// para entrar em expressão de índice.
    ///
    /// Índice PARCIAL pelo mesmo predicado da consulta (<c>numero ~ '^[0-9]+$'</c>): além de
    /// deixar o índice menor, é o que garante que o cast nunca é avaliado sobre um valor
    /// não-numérico durante a construção — sem isso, um único registro legado com letra no
    /// número faria o CREATE INDEX falhar.
    /// </summary>
    public partial class AddIndiceNumeracaoNotaFiscal : Migration
    {
        private const string Nome = "IX_notas_fiscais_tipo_serie_numero_int";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql($"""
                CREATE INDEX IF NOT EXISTS "{Nome}"
                    ON notas_fiscais (tipo, serie, ((numero)::integer) DESC)
                 WHERE numero ~ '^[0-9]+$';
                """);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql($"""DROP INDEX IF EXISTS "{Nome}";""");
    }
}
