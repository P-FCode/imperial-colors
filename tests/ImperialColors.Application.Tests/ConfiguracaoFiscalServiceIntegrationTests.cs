using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Regime tributário da empresa (tela Configurações → Fiscal): valor padrão quando nunca
/// configurado, e persistência ao salvar. Requer RUN_INTEGRATION_TESTS=true e PostgreSQL
/// acessível via .env.
/// </summary>
[Collection(ConfiguracaoFiscalGlobalCollection.Nome)]
public class ConfiguracaoFiscalServiceIntegrationTests
{
    [Fact]
    public async Task ObterRegimeAsync_SemConfiguracaoPrevia_RetornaSimplesNacionalComoPadrao()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await context.Database.MigrateAsync();

        // Remove qualquer configuração anterior de teste, se sobrou de uma execução passada.
        var parametro = await context.ParametrosSistema
            .FirstOrDefaultAsync(p => p.Chave == "RegimeTributarioEmpresa");
        if (parametro is not null)
        {
            context.ParametrosSistema.Remove(parametro);
            await context.SaveChangesAsync();
        }

        var configuracaoFiscal = provider.GetRequiredService<IConfiguracaoFiscalService>();
        var regime = await configuracaoFiscal.ObterRegimeAsync();

        Assert.Equal(RegimeTributario.SimplesNacional, regime);
    }

    [Theory]
    [InlineData(RegimeTributario.SimplesNacional)]
    [InlineData(RegimeTributario.LucroPresumido)]
    [InlineData(RegimeTributario.LucroReal)]
    [InlineData(RegimeTributario.Mei)]
    public async Task DefinirRegimeAsync_SalvaERecuperaOMesmoValor(RegimeTributario regimeEsperado)
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await context.Database.MigrateAsync();

        var configuracaoFiscal = provider.GetRequiredService<IConfiguracaoFiscalService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(regimeEsperado);
            var recuperado = await configuracaoFiscal.ObterRegimeAsync();

            Assert.Equal(regimeEsperado, recuperado);
        }
        finally
        {
            // Restaura o padrão para não vazar estado entre execuções de teste.
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
        }
    }

    /// <summary>
    /// CRT combina o regime (ParametroSistema) com SimplesExcessoSublimite (linha
    /// singleton ConfiguracaoFiscalEmpresa) — os quatro casos reais confirmados contra
    /// o contrato de uma API de emissão (CRT 1/2/3/4).
    /// </summary>
    [Theory]
    [InlineData(RegimeTributario.SimplesNacional, false, "1")]
    [InlineData(RegimeTributario.SimplesNacional, true, "2")]
    [InlineData(RegimeTributario.LucroPresumido, false, "3")]
    [InlineData(RegimeTributario.LucroReal, false, "3")]
    [InlineData(RegimeTributario.Mei, false, "4")]
    public async Task ObterCodigoCrtAsync_CombinaRegimeEExcessoSublimite(
        RegimeTributario regime, bool excessoSublimite, string crtEsperado)
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await context.Database.MigrateAsync();

        var configuracaoFiscal = provider.GetRequiredService<IConfiguracaoFiscalService>();

        try
        {
            await configuracaoFiscal.DefinirRegimeAsync(regime);
            await configuracaoFiscal.SalvarConfiguracaoEmpresaAsync(new ConfiguracaoFiscalEmpresaDto
            {
                SimplesExcessoSublimite = excessoSublimite
            });

            var crt = await configuracaoFiscal.ObterCodigoCrtAsync();
            Assert.Equal(crtEsperado, crt);
        }
        finally
        {
            await configuracaoFiscal.DefinirRegimeAsync(RegimeTributario.SimplesNacional);
            await configuracaoFiscal.SalvarConfiguracaoEmpresaAsync(new ConfiguracaoFiscalEmpresaDto
            {
                SimplesExcessoSublimite = false
            });
        }
    }

    [Fact]
    public async Task SalvarConfiguracaoEmpresaAsync_SalvaERecuperaEnderecoFiscalCompleto()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();
        using var context = provider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
        await context.Database.MigrateAsync();

        var configuracaoFiscal = provider.GetRequiredService<IConfiguracaoFiscalService>();

        var dto = new ConfiguracaoFiscalEmpresaDto
        {
            Cep = "80010000",
            Logradouro = "Rua Teste",
            Numero = "100",
            Bairro = "Centro",
            CodigoMunicipioIbge = "4106902",
            NomeMunicipio = "Curitiba",
            Uf = "PR",
            Serie = "1",
            Ambiente = AmbienteEmissaoFiscal.Homologacao,
            IdCscHomologacao = "000001",
            CscHomologacao = "csc-de-teste",
            AliquotaIbsUfPadrao = 0.10m,
            AliquotaIbsMunicipioPadrao = 0.00m,
            AliquotaCbsPadrao = 0.90m
        };

        var salvo = await configuracaoFiscal.SalvarConfiguracaoEmpresaAsync(dto);
        Assert.True(salvo.EnderecoPreenchido);
        Assert.Equal("4106902", salvo.CodigoMunicipioIbge);

        var recuperado = await configuracaoFiscal.ObterConfiguracaoEmpresaAsync();
        Assert.Equal("80010000", recuperado.Cep);
        Assert.Equal("PR", recuperado.Uf);
        Assert.Equal(AmbienteEmissaoFiscal.Homologacao, recuperado.Ambiente);
        Assert.Equal("000001", recuperado.IdCscHomologacao);
        Assert.Equal(0.90m, recuperado.AliquotaCbsPadrao);
    }
}
