using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class PagamentoHelperTests
{
    [Theory]
    [InlineData(FormaPagamento.Dinheiro, 100, 150, 1, 150, 50, 1)]
    [InlineData(FormaPagamento.Pix, 100, 0, 1, 100, 0, 1)]
    [InlineData(FormaPagamento.CartaoCredito, 200, 0, 3, 200, 0, 3)]
    public void CalcularPagamento_RetornaValoresCorretos(
        FormaPagamento forma, decimal total, decimal recebido, int parcelas,
        decimal valorPagoEsperado, decimal trocoEsperado, int parcelasEsperadas)
    {
        var (valorPago, troco, parcelasFinal) =
            ImperialColors.Application.Helpers.PagamentoHelper.CalcularPagamento(forma, total, recebido, parcelas);

        Assert.Equal(valorPagoEsperado, valorPago);
        Assert.Equal(trocoEsperado, troco);
        Assert.Equal(parcelasEsperadas, parcelasFinal);
    }

    [Fact]
    public void ValidarPagamento_DinheiroInsuficiente_LancaDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ImperialColors.Application.Helpers.PagamentoHelper.ValidarPagamento(
                FormaPagamento.Dinheiro, 100, 50, 1));

        Assert.Contains("insuficiente", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ObterDescricao_CreditoParcelado_RetornaTextoFormatado()
    {
        var descricao = ImperialColors.Application.Helpers.PagamentoHelper.ObterDescricao(
            FormaPagamento.CartaoCredito, 3);

        Assert.Equal("Cartão de Crédito - 3x", descricao);
    }

    [Fact]
    public void ValidarPagamentosCompostos_SomaCorreta_NaoLancaExcecao()
    {
        var pagamentos = new List<ImperialColors.Application.DTOs.CriarVendaPagamentoDto>
        {
            new() { FormaPagamento = FormaPagamento.Dinheiro, Valor = 50m, ValorRecebido = 50m },
            new() { FormaPagamento = FormaPagamento.Pix, Valor = 100m }
        };

        ImperialColors.Application.Helpers.PagamentoHelper.ValidarPagamentosCompostos(150m, pagamentos);
    }

    [Fact]
    public void ValidarPagamentosCompostos_SomaIncorreta_LancaDomainException()
    {
        var pagamentos = new List<ImperialColors.Application.DTOs.CriarVendaPagamentoDto>
        {
            new() { FormaPagamento = FormaPagamento.Dinheiro, Valor = 50m, ValorRecebido = 50m }
        };

        Assert.Throws<DomainException>(() =>
            ImperialColors.Application.Helpers.PagamentoHelper.ValidarPagamentosCompostos(150m, pagamentos));
    }

    [Fact]
    public void NormalizarPagamentos_LegacySinglePayment_RetornaUmItem()
    {
        var dto = new ImperialColors.Application.DTOs.CriarVendaDto
        {
            FormaPagamento = FormaPagamento.Pix,
            ValorPago = 100m
        };

        var pagamentos = ImperialColors.Application.Helpers.PagamentoHelper.NormalizarPagamentos(dto, 100m);

        Assert.Single(pagamentos);
        Assert.Equal(FormaPagamento.Pix, pagamentos[0].FormaPagamento);
        Assert.Equal(100m, pagamentos[0].Valor);
    }
}
