using ImperialColors.Domain.Entities;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Confirma que o <c>ValueConverter</c> de <c>ConfiguracaoFiscalEmpresaMapping</c> está de fato
/// no caminho: o segredo precisa chegar CIFRADO na coluna e voltar EM CLARO pela entidade.
/// Um teste só sobre <c>ProtecaoSegredoFiscal</c> não pegaria o erro mais provável aqui, que é
/// esquecer de aplicar o conversor a uma das três colunas.
///
/// Roda dentro de uma transação revertida — não deixa rastro no banco de desenvolvimento.
/// Na coleção da configuração fiscal global (convenção do CLAUDE.md): escreve na linha única
/// de <c>configuracao_fiscal_empresa</c> e, mesmo revertendo, o lock dessa linha travaria
/// qualquer outro teste fiscal rodando em paralelo.
/// </summary>
[Collection(ConfiguracaoFiscalGlobalCollection.Nome)]
public class SegredoFiscalPersistenciaIntegrationTests
{
    private const string CscExemplo = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890";
    private const string ApiKeyExemplo = "pfc_live_5f4dcc3b5aa765d61d8327deb882cf99";

    [Fact]
    public async Task SegredosVaoCifradosParaOBancoEVoltamEmClaro()
    {
        // Mesmo padrão dos demais testes de integração: sem RUN_INTEGRATION_TESTS=true,
        // simplesmente não roda (ver IntegrationTestGuard).
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        var config = await ctx.ConfiguracoesFiscalEmpresa.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new ConfiguracaoFiscalEmpresa();
            ctx.ConfiguracoesFiscalEmpresa.Add(config);
        }

        config.ApiKeyFiscal = ApiKeyExemplo;
        config.CscProducao = CscExemplo;
        config.CscHomologacao = CscExemplo;
        await ctx.SaveChangesAsync();

        // Lê a coluna crua, por fora do EF, para ver o que realmente ficou gravado.
        var cru = await ctx.Database
            .SqlQuery<string>($"SELECT api_key_fiscal AS \"Value\" FROM configuracao_fiscal_empresa WHERE id = {config.Id}")
            .SingleAsync();

        Assert.StartsWith("dpapi:v1:", cru);
        Assert.DoesNotContain(ApiKeyExemplo, cru);
        Assert.DoesNotContain("5f4dcc3b", cru);

        // E pela entidade tem que voltar legível, senão a emissão quebraria.
        ctx.ChangeTracker.Clear();
        var relido = await ctx.ConfiguracoesFiscalEmpresa.AsNoTracking().FirstAsync(c => c.Id == config.Id);

        Assert.Equal(ApiKeyExemplo, relido.ApiKeyFiscal);
        Assert.Equal(CscExemplo, relido.CscProducao);
        Assert.Equal(CscExemplo, relido.CscHomologacao);

        await tx.RollbackAsync();
    }
}
