using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class TabelaPrecosHelperTests
{
    [Fact]
    public void CalcularPrecoExibicao_SemAcrescimo_RetornaPrecoOriginal()
    {
        Assert.Equal(100m, TabelaPrecosHelper.CalcularPrecoExibicao(100m));
    }

    [Fact]
    public void CalcularPrecoExibicao_TabelaPintor_AplicaCincoPorcento()
    {
        var preco = TabelaPrecosHelper.CalcularPrecoExibicao(
            100m,
            TabelaPrecosHelper.AcrescimoTabelaPintorPercentual);

        Assert.Equal(105m, preco);
    }

    [Fact]
    public void ObterCodigoBarrasExibicao_PriorizaCodigoDeBarras()
    {
        Assert.Equal("789123", TabelaPrecosHelper.ObterCodigoBarrasExibicao("789123", "P001"));
    }

    [Fact]
    public void ObterCodigoBarrasExibicao_UsaCodigoInternoQuandoBarrasVazio()
    {
        Assert.Equal("P001", TabelaPrecosHelper.ObterCodigoBarrasExibicao(null, "P001"));
    }
}
