using ImperialColors.Domain.Entities;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Prova que o retry transacional funciona de verdade, e não apenas compila.
///
/// Não basta ligar <c>EnableRetryOnFailure</c>: com estratégia de retry ativa, o EF lança
/// <c>InvalidOperationException</c> no primeiro <c>SaveChanges</c> dentro de um
/// <c>BeginTransaction</c> manual. O que torna a combinação válida é
/// <c>RepositoryBase.ExecutarEmTransacaoAsync</c>, que roda tudo sob
/// <c>CreateExecutionStrategy</c> e cria um <c>DbContext</c> novo por tentativa.
///
/// O teste provoca uma falha transitória REAL — o backend derruba a própria conexão com
/// <c>pg_terminate_backend</c>, o que o Postgres reporta como <c>57P01</c> e o Npgsql
/// classifica como transitório — e confere as duas coisas que importam:
///   1. a operação se recupera sozinha (não propaga exceção);
///   2. o efeito acontece EXATAMENTE UMA VEZ (a tentativa abortada não deixa resíduo).
///
/// O segundo ponto é o que realmente interessa num PDV: retry que duplica venda é pior que
/// não ter retry.
/// </summary>
public class RetryTransacionalIntegrationTests
{
    /// <summary>Expõe o método protegido de <see cref="RepositoryBase{T}"/> para o teste.</summary>
    private sealed class RepositorioDeTeste(IDbContextFactory<AppDbContext> factory)
        : RepositoryBase<Categoria>(factory)
    {
        public Task<T> ExecutarAsync<T>(Func<AppDbContext, Task<T>> operacao, CancellationToken ct)
            => ExecutarEmTransacaoAsync(operacao, ct);
    }

    [Fact]
    public async Task FalhaTransitoriaNoMeioDaTransacao_RefazSozinhaESemDuplicar()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        // Factory própria com retry ligado, espelhando o que AddInfrastructure configura.
        var factory = new FactoryDeTeste(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(conexao, npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(1), errorCodesToAdd: null))
            .Options);

        var repositorio = new RepositorioDeTeste(factory);
        var nome = $"__retry_{Guid.NewGuid():N}"[..30];
        var tentativas = 0;

        try
        {
            var id = await repositorio.ExecutarAsync(async context =>
            {
                tentativas++;

                context.Categorias.Add(new Categoria { Nome = nome });
                await context.SaveChangesAsync();

                // Só a PRIMEIRA tentativa se sabota: mata o próprio backend, derrubando a
                // conexão e abortando a transação que acabou de inserir a categoria.
                if (tentativas == 1)
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "SELECT pg_terminate_backend(pg_backend_pid())");
                }

                return await context.Categorias
                    .Where(c => c.Nome == nome)
                    .Select(c => c.Id)
                    .FirstAsync();
            }, CancellationToken.None);

            Assert.True(tentativas >= 2, $"A estratégia deveria ter repetido; tentativas = {tentativas}.");
            Assert.True(id > 0);

            // O ponto central: a inserção da tentativa abortada não sobreviveu ao rollback.
            await using var conferencia = factory.CreateDbContext();
            var quantas = await conferencia.Categorias.IgnoreQueryFilters()
                .CountAsync(c => c.Nome == nome);

            Assert.Equal(1, quantas);
        }
        finally
        {
            await using var limpeza = factory.CreateDbContext();
            await limpeza.Categorias.IgnoreQueryFilters()
                .Where(c => c.Nome == nome)
                .ExecuteDeleteAsync();
        }
    }

    private sealed class FactoryDeTeste(DbContextOptions<AppDbContext> opcoes)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(opcoes);
    }
}
