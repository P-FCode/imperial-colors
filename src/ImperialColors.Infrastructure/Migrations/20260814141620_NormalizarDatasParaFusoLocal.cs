using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImperialColors.Infrastructure.Migrations
{
    /// <summary>
    /// Converte para horário LOCAL as colunas que até então eram gravadas em UTC.
    ///
    /// Contexto: todas as colunas de data são <c>timestamp without time zone</c>, mas metade
    /// do sistema gravava <c>DateTime.Now</c> (local) e a outra metade <c>DateTime.UtcNow</c>.
    /// Uma venda às 21h30 de 14/08 ficava com <c>data_venda = 14/08 21:30</c> e
    /// <c>criado_em = 15/08 00:30</c> na mesma linha. Ver <c>Domain.Helpers.Relogio</c> para
    /// o porquê de o padrão escolhido ser o horário local.
    ///
    /// Colunas convertidas (eram UTC):
    ///   • <c>criado_em</c> e <c>atualizado_em</c> — em TODAS as tabelas (vinham de
    ///     <c>BaseEntity</c> e de <c>AppDbContext.AtualizarTimestamps</c>);
    ///   • <c>logs_auditoria.data_hora</c>, <c>trocas.data_troca</c>,
    ///     <c>usuarios.data_cadastro</c> — defaults próprios das entidades.
    ///
    /// Colunas deliberadamente NÃO tocadas (já eram locais, ou não são instantes):
    ///   • <c>vendas.data_venda</c>, <c>vendas_externas.data_venda</c>,
    ///     <c>notas_fiscais.data_emissao</c>, <c>nota_fiscal_eventos.data_hora</c> — sempre
    ///     usaram <c>DateTime.Now</c>. Atenção: <c>data_hora</c> existe em duas tabelas com
    ///     origens opostas — em <c>logs_auditoria</c> era UTC, em <c>nota_fiscal_eventos</c>
    ///     era local. Por isso a conversão é nomeada tabela a tabela, nunca por nome de coluna.
    ///   • <c>notas_fiscais.dh_recbto</c> — vem da API fiscal já convertida para local
    ///     (<c>FiscalApiClient.ParseDataHora</c> usa <c>.LocalDateTime</c>);
    ///   • <c>notas_fiscais.data_saida</c>, <c>produtos.data_validade</c>,
    ///     <c>parametros_sistema.valor_data</c> — datas digitadas/meia-noite, sem hora real.
    ///
    /// <c>AT TIME ZONE 'UTC' AT TIME ZONE 'America/Sao_Paulo'</c> interpreta o valor como UTC
    /// e devolve o instante equivalente em Curitiba/PR — respeitando o histórico de horário
    /// de verão (extinto em 2019), o que um <c>- interval '3 hours'</c> fixo erraria em
    /// registros anteriores a essa data.
    /// </summary>
    public partial class NormalizarDatasParaFusoLocal : Migration
    {
        private const string FusoLocal = "America/Sao_Paulo";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(MontarSql(deUtcParaLocal: true));

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(MontarSql(deUtcParaLocal: false));

        private static string MontarSql(bool deUtcParaLocal)
        {
            // Ida: naive-UTC -> naive-local.  Volta: naive-local -> naive-UTC.
            var origem = deUtcParaLocal ? "UTC" : FusoLocal;
            var destino = deUtcParaLocal ? FusoLocal : "UTC";

            return $"""
                DO $$
                DECLARE
                    r RECORD;
                BEGIN
                    -- criado_em / atualizado_em são uniformes em todo o esquema: sempre vieram
                    -- de BaseEntity/AtualizarTimestamps, sempre em UTC. Percorrer o catálogo
                    -- evita listar 21 tabelas à mão e não deixa nenhuma para trás quando uma
                    -- entidade nova for adicionada antes desta migration rodar no cliente.
                    FOR r IN
                        SELECT table_name, column_name
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND column_name IN ('criado_em', 'atualizado_em')
                          AND data_type = 'timestamp without time zone'
                    LOOP
                        EXECUTE format(
                            'UPDATE %I SET %I = (%I AT TIME ZONE %L) AT TIME ZONE %L WHERE %I IS NOT NULL',
                            r.table_name, r.column_name, r.column_name, '{origem}', '{destino}', r.column_name);
                    END LOOP;
                END $$;

                UPDATE logs_auditoria
                   SET data_hora = (data_hora AT TIME ZONE '{origem}') AT TIME ZONE '{destino}'
                 WHERE data_hora IS NOT NULL;

                UPDATE trocas
                   SET data_troca = (data_troca AT TIME ZONE '{origem}') AT TIME ZONE '{destino}'
                 WHERE data_troca IS NOT NULL;

                UPDATE usuarios
                   SET data_cadastro = (data_cadastro AT TIME ZONE '{origem}') AT TIME ZONE '{destino}'
                 WHERE data_cadastro IS NOT NULL;
                """;
        }
    }
}
