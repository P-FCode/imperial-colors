using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Índices de trigrama em <c>vendas</c>, agora que a busca daquela tela sabe usá-los.
    ///
    /// Estes dois índices foram deliberadamente DEIXADOS DE FORA da migration
    /// <c>AddIndicesBuscaTextual</c>, e o motivo está documentado lá: com o formato antigo da
    /// consulta — um único <c>WHERE</c> com <c>numero_venda OR cliente.nome OR
    /// nome_comprador_cupom</c> — o OR atravessava o JOIN com <c>clientes</c> e nunca poderia
    /// virar BitmapOr. Verificado na época: mesmo com os índices criados, o plano continuava
    /// Nested Loop com filtro pós-junção. Criá-los teria adicionado custo de escrita na tabela
    /// de maior volume de inserção do sistema, em troca de nada.
    ///
    /// O que mudou: <c>ObterPaginadoPorPeriodoAsync</c> passou a montar a busca como UNION de
    /// três ramos independentes, cada um filtrando uma única tabela. Agora cada ramo usa o
    /// índice próprio, e estes dois passam a ser exercitados de verdade — o ramo do cliente
    /// usa o GIN de <c>clientes.nome</c> (que já existe) mais o índice da FK
    /// <c>vendas.cliente_id</c>.
    ///
    /// Medido com 200 mil vendas e 50 mil clientes, termo seletivo: 152 ms → 0,63 ms.
    /// </summary>
    public partial class AddIndicesBuscaVendas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_vendas_numero_venda_trgm"
                    ON vendas USING gin (numero_venda gin_trgm_ops);

                -- Parcial: só a minoria das vendas é de cupom avulso com nome digitado na
                -- hora; nas demais a coluna é NULL e não teria entrada no índice de qualquer
                -- forma. O predicado deixa isso explícito e mantém o índice menor.
                CREATE INDEX IF NOT EXISTS "IX_vendas_nome_comprador_cupom_trgm"
                    ON vendas USING gin (nome_comprador_cupom gin_trgm_ops)
                 WHERE nome_comprador_cupom IS NOT NULL;
                """);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_vendas_nome_comprador_cupom_trgm";
                DROP INDEX IF EXISTS "IX_vendas_numero_venda_trgm";
                """);
    }
}
