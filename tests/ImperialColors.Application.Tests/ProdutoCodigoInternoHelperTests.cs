using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ProdutoCodigoInternoHelperTests
{
    [Theory]
    [InlineData(1, "P00001")]
    [InlineData(42, "P00042")]
    [InlineData(99999, "P99999")]
    public void FormatarSequencia_DeveGerarCodigoPadrao(int sequencia, string esperado)
    {
        Assert.Equal(esperado, ProdutoCodigoInternoHelper.FormatarSequencia(sequencia));
    }

    [Theory]
    [InlineData("P00001", true)]
    [InlineData("P99999", true)]
    [InlineData("P00000", false)]
    [InlineData("PRD-0001", false)]
    [InlineData("X00001", false)]
    public void EhCodigoSequencialPadrao_DeveValidarFormato(string codigo, bool esperado)
    {
        Assert.Equal(esperado, ProdutoCodigoInternoHelper.EhCodigoSequencialPadrao(codigo));
    }
}
