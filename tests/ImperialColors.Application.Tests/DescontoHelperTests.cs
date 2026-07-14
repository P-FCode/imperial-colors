using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class DescontoHelperTests
{
    [Fact]
    public void CalcularDescontoPercentual_10PorCentoDe200_DeveRetornar20()
    {
        var desconto = DescontoHelper.CalcularDescontoPercentual(200m, 10m);
        Assert.Equal(20m, desconto);
    }

    [Fact]
    public void CalcularTotalLiquido_200Com10PorCento_DeveRetornar180()
    {
        var desconto = DescontoHelper.CalcularDescontoEmReais(200m, 10m, TipoDescontoVenda.Percentual);
        var total = DescontoHelper.CalcularTotalLiquido(200m, desconto);
        Assert.Equal(180m, total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void PercentualValido_ForaDoIntervalo_DeveSerInvalido(decimal percentual)
    {
        Assert.False(DescontoHelper.PercentualValido(percentual));
    }

    [Fact]
    public void CalcularDescontoEmReais_ValorFixoNaoPodeExcederSubtotal()
    {
        var desconto = DescontoHelper.CalcularDescontoEmReais(100m, 150m, TipoDescontoVenda.Valor);
        Assert.Equal(100m, desconto);
    }
}
