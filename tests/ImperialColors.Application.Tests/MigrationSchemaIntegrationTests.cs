using Npgsql;
using Xunit;

namespace ImperialColors.Application.Tests;

public class MigrationSchemaIntegrationTests
{
    [Fact]
    public async Task RemoveUnidadeCustoAndRestoreClientes_DeveEstarAplicadaNoBanco()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var connectionString))
            return;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        await using (var cmdMigration = new NpgsqlCommand(
                         """
                         SELECT COUNT(*)
                         FROM "__EFMigrationsHistory"
                         WHERE "MigrationId" = '20260617133010_RemoveUnidadeCustoAndClientes'
                         """,
                         conn))
        {
            var count = Convert.ToInt32(await cmdMigration.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }

        await using (var cmdUnidadeCusto = new NpgsqlCommand(
                         """
                         SELECT COUNT(*)
                         FROM information_schema.columns
                         WHERE table_name = 'produtos' AND column_name = 'unidade_custo'
                         """,
                         conn))
        {
            var count = Convert.ToInt32(await cmdUnidadeCusto.ExecuteScalarAsync());
            Assert.Equal(0, count);
        }

        await using (var cmdClientes = new NpgsqlCommand(
                         """
                         SELECT COUNT(*)
                         FROM information_schema.tables
                         WHERE table_name = 'clientes'
                         """,
                         conn))
        {
            var count = Convert.ToInt32(await cmdClientes.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }

        await using (var cmdClienteId = new NpgsqlCommand(
                         """
                         SELECT COUNT(*)
                         FROM information_schema.columns
                         WHERE table_name = 'vendas' AND column_name = 'cliente_id'
                         """,
                         conn))
        {
            var count = Convert.ToInt32(await cmdClienteId.ExecuteScalarAsync());
            Assert.Equal(1, count);
        }
    }
}
