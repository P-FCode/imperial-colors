using ImperialColors.Infrastructure.Configuration;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// De onde o sistema instalado busca as releases. O valor saiu do código para o <c>.env</c>
/// justamente porque o dia de trocar o projeto de conta é o dia em que ele precisa mudar sem
/// depender de uma release — que o cliente buscaria no repositório antigo.
///
/// Por isso a leitura é tolerante: valor vazio ou mal digitado cai no padrão conhecido, em
/// vez de deixar a máquina sem repositório nenhum para consultar.
/// </summary>
public class AtualizacaoConfigTests
{
    [Theory]
    [InlineData("OutroPerfil/imperial-colors", "OutroPerfil", "imperial-colors")]
    [InlineData("  OutroPerfil / imperial-colors  ", "OutroPerfil", "imperial-colors")]
    // Copiar da barra do navegador traz a barra final junto.
    [InlineData("OutroPerfil/imperial-colors/", "OutroPerfil", "imperial-colors")]
    public void ValorNoFormatoDonoBarraRepositorio_EhInterpretado(string valor, string dono, string repo)
    {
        var config = AtualizacaoConfig.Interpretar(valor);

        Assert.Equal(dono, config.Proprietario);
        Assert.Equal(repo, config.Repositorio);
        Assert.Equal($"{dono}/{repo}", config.Caminho);
        Assert.True(config.ConfiguradoPeloAmbiente);
    }

    [Theory]
    [InlineData(null)]           // variável ausente no .env
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("imperial-colors")]                       // faltou o dono
    [InlineData("perfil/repo/extra")]                     // colou a URL inteira
    [InlineData("https://github.com/perfil/repo")]        // idem, com protocolo
    public void ValorAusenteOuForaDoFormato_CaiNoPadrao(string? valor)
    {
        var config = AtualizacaoConfig.Interpretar(valor);

        Assert.Equal(AtualizacaoConfig.ProprietarioPadrao, config.Proprietario);
        Assert.Equal(AtualizacaoConfig.RepositorioPadrao, config.Repositorio);
        // O log da verificação usa isso para dizer que o valor veio do padrão, e não do .env
        // — é como se descobre, numa troca de conta, que a máquina ainda não foi migrada.
        Assert.False(config.ConfiguradoPeloAmbiente);
    }

    /// <summary>Sem a variável no ambiente, o comportamento é exatamente o de antes de ela
    /// existir: o repositório que sempre valeu.</summary>
    [Fact]
    public void CarregarDoAmbiente_SemAVariavel_UsaORepositorioDeSempre()
    {
        var anterior = Environment.GetEnvironmentVariable(AtualizacaoConfig.Variavel);
        Environment.SetEnvironmentVariable(AtualizacaoConfig.Variavel, null);
        try
        {
            var config = AtualizacaoConfig.CarregarDoAmbiente();

            Assert.Equal("P-FCode/imperial-colors", config.Caminho);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AtualizacaoConfig.Variavel, anterior);
        }
    }

    [Fact]
    public void CarregarDoAmbiente_ComAVariavel_UsaORepositorioDoEnv()
    {
        var anterior = Environment.GetEnvironmentVariable(AtualizacaoConfig.Variavel);
        Environment.SetEnvironmentVariable(AtualizacaoConfig.Variavel, "NovoPerfil/sistema-loja");
        try
        {
            var config = AtualizacaoConfig.CarregarDoAmbiente();

            Assert.Equal("NovoPerfil/sistema-loja", config.Caminho);
            Assert.True(config.ConfiguradoPeloAmbiente);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AtualizacaoConfig.Variavel, anterior);
        }
    }
}
