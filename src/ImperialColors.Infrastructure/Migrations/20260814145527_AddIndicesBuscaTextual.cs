using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Índices de trigrama (pg_trgm/GIN) para as buscas <c>ILIKE '%termo%'</c> das telas.
    ///
    /// Um B-tree comum não serve para <c>'%termo%'</c>: sem âncora à esquerda, não há prefixo
    /// por onde descer a árvore, e o Postgres cai em varredura sequencial. O índice de
    /// trigrama quebra o texto em trincas de caracteres e permite localizar o padrão em
    /// qualquer posição. Medido com 200 mil clientes: 69,4 ms / 2.706 buffers →
    /// 0,17 ms / 15 buffers.
    ///
    /// ⚠️ Regra do OR: quando a busca é
    /// <c>colA ILIKE x OR colB ILIKE x OR colC ILIKE x</c>, o Postgres só evita a varredura se
    /// TODAS as colunas do OR tiverem índice — aí ele combina os resultados com BitmapOr.
    /// Indexar só uma delas não adianta nada. Confirmado ao vivo em logs_auditoria com 200 mil
    /// linhas: só descricao indexada → Seq Scan, 336 ms; as três indexadas → BitmapOr, 0,16 ms.
    /// É por isso que as três colunas de logs_auditoria entram juntas aqui, ou nenhuma valeria.
    ///
    /// ⚠️ vendas ficou DE FORA de propósito. A busca da tela de Vendas é
    /// <c>numero_venda ILIKE x OR cliente.nome ILIKE x OR nome_comprador_cupom ILIKE x</c> —
    /// e o segundo termo está em OUTRA TABELA. Um OR que atravessa JOIN não pode ser resolvido
    /// por BitmapOr, então os índices ficariam parados. Verificado: mesmo com GIN nas três
    /// colunas o plano continuou Nested Loop com filtro pós-junção, 160 ms. Criá-los só
    /// adicionaria custo de escrita na tabela de maior volume de inserção do sistema, sem
    /// nenhum ganho de leitura. Resolver aquela busca exige reescrever a consulta (UNION de
    /// ramos indexados, ou uma coluna de busca desnormalizada em vendas) — não é trabalho de
    /// migration.
    ///
    /// fornecedores também ficou de fora: são centenas de linhas, o Seq Scan é mais barato que
    /// o índice e o planner não o usaria.
    ///
    /// Nota operacional: <c>CREATE INDEX</c> comum (não CONCURRENTLY) porque migration do EF
    /// roda dentro de transação, e CONCURRENTLY não é permitido nela. Bloqueia escrita na
    /// tabela enquanto constrói — irrelevante agora (tabelas pequenas, roda no startup logo
    /// após a atualização), mas se um dia for aplicado sobre uma base já grande, vale rodar os
    /// CREATE INDEX CONCURRENTLY à mão antes e deixar o IF NOT EXISTS daqui virar no-op.
    /// </summary>
    public partial class AddIndicesBuscaTextual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pg_trgm;

                -- Busca de cliente: PDV e tela de emissão de NF-e.
                CREATE INDEX IF NOT EXISTS "IX_clientes_nome_trgm"
                    ON clientes USING gin (nome gin_trgm_ops);

                -- Busca de produto: caminho mais quente do sistema (PDV).
                -- O OR de ObterPaginadoAsync combina este GIN com os B-tree já existentes de
                -- codigo_interno e codigo_barras via BitmapOr.
                CREATE INDEX IF NOT EXISTS "IX_produtos_nome_trgm"
                    ON produtos USING gin (nome gin_trgm_ops);

                -- As três juntas: ver a regra do OR no sumário desta migration.
                CREATE INDEX IF NOT EXISTS "IX_logs_auditoria_descricao_trgm"
                    ON logs_auditoria USING gin (descricao gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_logs_auditoria_nome_usuario_trgm"
                    ON logs_auditoria USING gin (nome_usuario gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_logs_auditoria_acao_trgm"
                    ON logs_auditoria USING gin (acao gin_trgm_ops);

                -- Vendas externas: o OR é todo dentro da mesma tabela, então o BitmapOr funciona.
                CREATE INDEX IF NOT EXISTS "IX_vendas_externas_numero_trgm"
                    ON vendas_externas USING gin (numero_venda_externa gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS "IX_vendas_externas_observacoes_trgm"
                    ON vendas_externas USING gin (observacoes gin_trgm_ops);
                """);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_vendas_externas_observacoes_trgm";
                DROP INDEX IF EXISTS "IX_vendas_externas_numero_trgm";
                DROP INDEX IF EXISTS "IX_logs_auditoria_acao_trgm";
                DROP INDEX IF EXISTS "IX_logs_auditoria_nome_usuario_trgm";
                DROP INDEX IF EXISTS "IX_logs_auditoria_descricao_trgm";
                DROP INDEX IF EXISTS "IX_produtos_nome_trgm";
                DROP INDEX IF EXISTS "IX_clientes_nome_trgm";
                -- A extensão pg_trgm não é removida: pode estar em uso por outro objeto.
                """);
    }
}
