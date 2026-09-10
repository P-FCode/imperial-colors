using ImperialColors.Domain.Constants;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// BA (balde) e LA (lata) entraram na importação do catálogo Paraná — a mensagem de erro do
/// validador de produto (<c>ProdutoValidator</c>) passou a listar <see cref="UnidadesMedida.Todas"/>
/// diretamente em vez de um texto escrito à mão, exatamente para não repetir o que já
/// aconteceu aqui: a mensagem antiga citava "UN, GL, LT, RL, CX ou PCT" e nem mencionava BD,
/// que já existia havia tempo.
/// </summary>
public class UnidadesMedidaTests
{
    [Theory]
    [InlineData("BA")]
    [InlineData("ba")]
    [InlineData("LA")]
    [InlineData("la")]
    [InlineData("BD")]
    public void UnidadesDeEmbalagemDeTinta_SaoValidas(string unidade)
    {
        Assert.True(UnidadesMedida.EhValida(unidade));
    }

    [Fact]
    public void Normalizar_AceitaBaELaEmQualquerCaixa()
    {
        Assert.Equal("BA", UnidadesMedida.Normalizar("ba"));
        Assert.Equal("LA", UnidadesMedida.Normalizar(" la "));
    }
}
