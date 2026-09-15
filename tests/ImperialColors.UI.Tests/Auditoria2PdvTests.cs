using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.UI.Helpers;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// AUDITORIA (15/09, segunda rodada) — cálculos e formatação do PDV.
/// Reproduz, com as mesmas funções que PDVView usa, o caminho de tela.
/// </summary>
public class Auditoria2PdvTests
{
    /// <summary>
    /// CORRIGIDO (A4). Antes, 1,5 L a R$ 9,99 dava total 14,985 em memória: a tela recusava o valor
    /// sugerido (R$ 14,99) e R$ 14,98 deixava 0,005 impossível de pagar. Agora PDVView
    /// (RecalcularSubtotal), ItemVenda.CalcularSubtotal e a contingência arredondam para centavos.
    /// </summary>
    [Theory]
    [InlineData(1.5, 9.99, 14.99)]
    [InlineData(0.333, 45.90, 15.28)]
    [InlineData(2.75, 33.33, 91.66)]
    public void QuantidadeFracionada_ComPrecoQuebrado_TemQueExistirValorEmCentavosQueFechaAVenda(
        double quantidade, double preco, double totalEsperado)
    {
        var qtd = (decimal)quantidade;
        var precoUnitario = (decimal)preco;

        var totalNaTela = ArredondamentoHelper.Centavos(qtd * precoUnitario);
        var itemServidor = new ItemVenda { Quantidade = qtd, PrecoUnitario = precoUnitario };
        itemServidor.CalcularSubtotal();

        Assert.True(FormattingHelper.TryParseMoeda(FormattingHelper.FormatarMoedaEntrada(totalNaTela), out var sugerido));

        Assert.Equal((decimal)totalEsperado, totalNaTela);
        Assert.Equal(totalNaTela, itemServidor.Subtotal);
        Assert.True(sugerido <= totalNaTela, $"valor sugerido {sugerido} maior que o saldo {totalNaTela}");

        var erro = Record.Exception(() => PagamentoHelper.ValidarPagamentosCompostos(totalNaTela,
            [new CriarVendaPagamentoDto { FormaPagamento = FormaPagamento.Pix, Valor = sugerido }]));
        Assert.False(erro is DomainException, erro?.Message);
    }

    /// <summary>
    /// CORRIGIDO (B3). Antes, "N1" mostrava 1,25 como "1,3" no cupom, no PDV e no estoque — e o
    /// formulário de produto regravava o valor arredondado ao salvar.
    /// </summary>
    [Theory]
    [InlineData(1.25, "1,25")]
    [InlineData(0.125, "0,125")]
    [InlineData(2.5, "2,5")]
    [InlineData(10, "10")]
    [InlineData(1500.75, "1.500,75")]
    public void QuantidadeFracionada_DeveAparecerComAsCasasGravadas(double quantidade, string esperado)
    {
        Assert.Equal(esperado, FormattingHelper.FormatarQuantidade((decimal)quantidade));
    }

    [Fact]
    public void QuantidadeComUnidade_TambemAparecemExata()
    {
        Assert.StartsWith("1,25 ", FormattingHelper.FormatarQuantidadeUnidade(1.250m, "LT"));
        Assert.StartsWith("0,125 ", FormattingHelper.FormatarQuantidadeUnidade(0.125m, "GL"));
    }
}
