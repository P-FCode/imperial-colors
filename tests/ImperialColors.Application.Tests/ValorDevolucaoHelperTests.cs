using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>M8 da auditoria de 15/09: a troca credita o que o cliente pagou, não o preço de tabela.</summary>
public class ValorDevolucaoHelperTests
{
    [Fact]
    public void SemDesconto_CreditaOPrecoUnitario()
    {
        Assert.Equal(33.33m, ValorDevolucaoHelper.ValorUnitarioPago(33.33m, 0m, 99.99m, 3m, 99.99m, 0m));
    }

    [Fact]
    public void DescontoNoItem_EntraNoValorPago()
    {
        // 2 × R$ 100 com R$ 20 de desconto no item → pagou R$ 90 cada.
        Assert.Equal(90m, ValorDevolucaoHelper.ValorUnitarioPago(100m, 20m, 180m, 2m, 180m, 0m));
    }

    [Fact]
    public void DescontoGeralDaVenda_ERateadoPeloPesoDoItem()
    {
        // Venda: item A 2 × R$ 100 (200) + item B R$ 50; desconto geral R$ 25 (10% de 250).
        // A leva 200/250 do desconto = R$ 20 → pagou R$ 180 por 2 → R$ 90 cada.
        Assert.Equal(90m, ValorDevolucaoHelper.ValorUnitarioPago(100m, 0m, 200m, 2m, 250m, 25m));
        Assert.Equal(45m, ValorDevolucaoHelper.ValorUnitarioPago(50m, 0m, 50m, 1m, 250m, 25m));
    }

    [Fact]
    public void DescontoNoItemENaVenda_SomamOsDois()
    {
        // Item 1 × R$ 100 com R$ 10 no item (subtotal 90); venda só com ele e R$ 9 de desconto geral.
        Assert.Equal(81m, ValorDevolucaoHelper.ValorUnitarioPago(100m, 10m, 90m, 1m, 90m, 9m));
    }

    [Fact]
    public void ValorFracionado_ArredondaParaCentavos()
    {
        // 3 unidades pagas R$ 100 no total (R$ 110 com R$ 10 de desconto geral) → R$ 33,33 cada.
        Assert.Equal(33.33m, ValorDevolucaoHelper.ValorUnitarioPago(36.67m, 0m, 110m, 3m, 110m, 10m));
    }
}
