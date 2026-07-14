using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ListaCompraWhatsAppHelperTests
{
    [Theory]
    [InlineData(8, "Bom dia")]
    [InlineData(11, "Bom dia")]
    [InlineData(12, "Boa tarde")]
    [InlineData(17, "Boa tarde")]
    [InlineData(18, "Olá")]
    [InlineData(22, "Olá")]
    public void ObterSaudacao_RespeitaHorario(int hora, string esperado)
    {
        var data = new DateTime(2026, 6, 20, hora, 30, 0);
        Assert.Equal(esperado, ListaCompraWhatsAppHelper.ObterSaudacao(data));
    }

    [Fact]
    public void MontarMensagemPedido_FormataItensLegiveis()
    {
        var mensagem = ListaCompraWhatsAppHelper.MontarMensagemPedido(
            "Tintas Brasil",
            new[]
            {
                new ItemListaCompraDto { NomeProduto = "Tinta Acrílica Fosca 3,6L", QuantidadeDesejada = 5, Unidade = "UN" },
                new ItemListaCompraDto { NomeProduto = "Galão de Verniz 18L", QuantidadeDesejada = 2, Unidade = "GL" }
            },
            new DateTime(2026, 6, 20, 10, 0, 0));

        Assert.Contains("Bom dia, Tintas Brasil!", mensagem);
        Assert.Contains("- 5 UN | Tinta Acrílica Fosca 3,6L", mensagem);
        Assert.Contains("- 2 GL | Galão de Verniz 18L", mensagem);
        Assert.Contains("Ficamos no aguardo do orçamento. Obrigado!", mensagem);
    }

    [Fact]
    public void NormalizarTelefoneWhatsApp_RemoveMascaraEAdicionaDdi()
    {
        Assert.Equal("5541999999999", ListaCompraWhatsAppHelper.NormalizarTelefoneWhatsApp("(41) 99999-9999", null));
    }
}
