using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// CRUD de Naturezas de Operação: criação, listagem, atualização, exclusão (soft-delete)
/// e as mesmas validações fiscais (CST × CSOSN, formato de CFOP) usadas em Produto/Categoria.
/// Requer RUN_INTEGRATION_TESTS=true e PostgreSQL acessível via .env. Está na mesma
/// collection do restante da suíte fiscal porque lê o regime tributário global
/// (ParametroSistema) que outros testes desta suíte também alteram.
/// </summary>
[Collection(ConfiguracaoFiscalGlobalCollection.Nome)]
public class NaturezaOperacaoServiceIntegrationTests
{
    private static bool TryCarregarConfig(out ServiceProvider provider)
    {
        provider = null!;
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        context.Database.Migrate();
        return true;
    }

    [Fact]
    public async Task CriarAtualizarRemover_CicloCompleto_FuncionaCorretamente()
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var configuracaoFiscal = scope.ServiceProvider.GetRequiredService<IConfiguracaoFiscalService>();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);

            var sufixo = Guid.NewGuid().ToString("N")[..8];
            var criada = await service.CriarAsync(new NaturezaOperacaoDto
            {
                Descricao = $"Venda dentro do estado {sufixo}",
                TipoOperacao = TipoOperacaoFiscal.Saida,
                Finalidade = FinalidadeNfe.Normal,
                ConsumidorFinal = true,
                Serie = "1",
                CsosnPadrao = "102",
                CfopDentroEstado = "5102",
                CfopForaEstado = "6102"
            });

            Assert.True(criada.Id > 0);
            Assert.Equal("102", criada.CsosnPadrao);

            var porId = await service.ObterPorIdAsync(criada.Id);
            Assert.NotNull(porId);
            Assert.Equal(criada.Descricao, porId!.Descricao);

            var todos = await service.ObterTodosAsync();
            Assert.Contains(todos, n => n.Id == criada.Id);

            criada.CfopDentroEstado = "5405";
            criada.ObservacoesPadrao = "Observação de teste";
            var atualizada = await service.AtualizarAsync(criada);
            Assert.Equal("5405", atualizada.CfopDentroEstado);
            Assert.Equal("Observação de teste", atualizada.ObservacoesPadrao);

            await service.RemoverAsync(criada.Id);
            var removida = await service.ObterPorIdAsync(criada.Id);
            Assert.Null(removida);
        }
        finally
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
        }
    }

    [Fact]
    public async Task CriarAsync_CstECsosnPreenchidosJuntos_LancaDomainException()
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var configuracaoFiscal = scope.ServiceProvider.GetRequiredService<IConfiguracaoFiscalService>();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);

            var dto = new NaturezaOperacaoDto
            {
                Descricao = "Natureza inválida",
                CsosnPadrao = "102",
                CstIcmsPadrao = "00"
            };

            await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(dto));
        }
        finally
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
        }
    }

    [Fact]
    public async Task CriarAsync_CsosnComRegimeLucroReal_LancaDomainException()
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var configuracaoFiscal = scope.ServiceProvider.GetRequiredService<IConfiguracaoFiscalService>();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.LucroReal);

            var dto = new NaturezaOperacaoDto
            {
                Descricao = "Natureza com CSOSN em regime normal",
                CsosnPadrao = "102"
            };

            await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(dto));
        }
        finally
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
        }
    }

    [Theory]
    [InlineData("610")]
    [InlineData("ABCD")]
    [InlineData("6102")]
    public async Task CriarAsync_CfopDentroEstadoForaDoPadrao_LancaDomainException(string cfopInvalido)
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var configuracaoFiscal = scope.ServiceProvider.GetRequiredService<IConfiguracaoFiscalService>();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);

            var dto = new NaturezaOperacaoDto
            {
                Descricao = "Natureza com CFOP inválido",
                CfopDentroEstado = cfopInvalido // deve começar com "5" e ter 4 dígitos
            };

            await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(dto));
        }
        finally
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
        }
    }

    [Fact]
    public async Task CriarAsync_DescricaoVazia_LancaDomainException()
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        var dto = new NaturezaOperacaoDto { Descricao = "   " };

        await Assert.ThrowsAsync<DomainException>(() => service.CriarAsync(dto));
    }

    [Fact]
    public async Task AtualizarAsync_IdInexistente_LancaDomainException()
    {
        if (!TryCarregarConfig(out var provider))
            return;

        await using var scope = provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<INaturezaOperacaoService>();

        var dto = new NaturezaOperacaoDto { Id = 999_999_999, Descricao = "Inexistente" };

        await Assert.ThrowsAsync<DomainException>(() => service.AtualizarAsync(dto));
    }
}
