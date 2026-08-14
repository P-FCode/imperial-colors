using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Homologação e produção são sequências de numeração independentes na SEFAZ — a chave de
/// acesso de 44 dígitos não tem sequer um dígito de ambiente, ele viaja no <c>tpAmb</c> do
/// XML. Enquanto <c>ObterProximoNumeroAsync</c> ignorava o ambiente, cada nota de teste
/// empurrava a numeração de produção para frente e vice-versa; como a numeração é imutável,
/// o número pulado nunca voltava e o buraco ficava permanente nas duas séries.
///
/// Roda contra o Postgres de verdade porque o que está sendo testado é a consulta SQL crua
/// (SqlQuery com MAX + CAST), não uma expressão LINQ traduzível em memória. Tudo dentro de
/// uma transação revertida; não deixa rastro no banco de desenvolvimento.
/// </summary>
public class NumeracaoFiscalPorAmbienteIntegrationTests
{
    private const string SerieDeTeste = "907";

    private static NotaFiscal Nota(AmbienteEmissaoFiscal ambiente, string numero, StatusNotaFiscal status) => new()
    {
        Tipo = TipoNotaFiscal.NFe,
        Serie = SerieDeTeste,
        Numero = numero,
        Ambiente = ambiente,
        Status = status,
        DataEmissao = DateTime.Now,
        Crt = "1"
    };

    [Fact]
    public async Task ProximoNumero_NaoEnxergaNotasDoOutroAmbiente()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        ctx.NotasFiscais.AddRange(
            Nota(AmbienteEmissaoFiscal.Producao, "1", StatusNotaFiscal.Autorizada),
            Nota(AmbienteEmissaoFiscal.Producao, "2", StatusNotaFiscal.Autorizada),
            // A nota de teste que, antes da correção, empurrava a produção de 3 para 51.
            Nota(AmbienteEmissaoFiscal.Homologacao, "50", StatusNotaFiscal.Autorizada));
        await ctx.SaveChangesAsync();

        var repositorio = new NotaFiscalRepository(new FactoryDeContextoFixo(ctx));

        var proximoProducao = await repositorio.ObterProximoNumeroAsync(
            TipoNotaFiscal.NFe, SerieDeTeste, AmbienteEmissaoFiscal.Producao);
        var proximoHomologacao = await repositorio.ObterProximoNumeroAsync(
            TipoNotaFiscal.NFe, SerieDeTeste, AmbienteEmissaoFiscal.Homologacao);

        Assert.Equal("3", proximoProducao);
        Assert.Equal("51", proximoHomologacao);

        await tx.RollbackAsync();
    }

    /// <summary>
    /// A regra que não pode se perder junto: dentro do MESMO ambiente, uma nota rejeitada
    /// continua consumindo o número. O filtro por ambiente é um recorte da sequência, não uma
    /// licença para reaproveitar numeração queimada.
    /// </summary>
    [Fact]
    public async Task ProximoNumero_ContinuaContandoNotaRejeitadaDoMesmoAmbiente()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        ctx.NotasFiscais.AddRange(
            Nota(AmbienteEmissaoFiscal.Producao, "10", StatusNotaFiscal.Autorizada),
            Nota(AmbienteEmissaoFiscal.Producao, "11", StatusNotaFiscal.Rejeitada),
            // Soft-deletada: o número segue queimado mesmo assim.
            new NotaFiscal
            {
                Tipo = TipoNotaFiscal.NFe,
                Serie = SerieDeTeste,
                Numero = "12",
                Ambiente = AmbienteEmissaoFiscal.Producao,
                Status = StatusNotaFiscal.Rejeitada,
                DataEmissao = DateTime.Now,
                Crt = "1",
                Ativo = false
            });
        await ctx.SaveChangesAsync();

        var repositorio = new NotaFiscalRepository(new FactoryDeContextoFixo(ctx));

        var proximo = await repositorio.ObterProximoNumeroAsync(
            TipoNotaFiscal.NFe, SerieDeTeste, AmbienteEmissaoFiscal.Producao);

        Assert.Equal("13", proximo);

        await tx.RollbackAsync();
    }
}
