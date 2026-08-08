using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class FormaPagamentoFiscalMapperTests
{
    [Theory]
    [InlineData(FormaPagamento.Dinheiro, "01")]
    [InlineData(FormaPagamento.CartaoCredito, "03")]
    [InlineData(FormaPagamento.CartaoDebito, "04")]
    [InlineData(FormaPagamento.Boleto, "15")]
    [InlineData(FormaPagamento.Pix, "17")]
    public void ParaTPag_RetornaCodigoOficialDaSefaz(FormaPagamento formaPagamento, string tPagEsperado)
        => Assert.Equal(tPagEsperado, FormaPagamentoFiscalMapper.ParaTPag(formaPagamento));

    [Fact]
    public void ParaTPag_TodosOsValoresDoEnumTemMapeamentoUnico()
    {
        // Garante que nenhum valor do enum cai no fallback "99" por acidente, e que
        // não existem dois valores do enum mapeando pro mesmo tPag.
        var valores = Enum.GetValues<FormaPagamento>();
        var codigos = valores.Select(FormaPagamentoFiscalMapper.ParaTPag).ToList();

        Assert.DoesNotContain("99", codigos);
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
    }
}
