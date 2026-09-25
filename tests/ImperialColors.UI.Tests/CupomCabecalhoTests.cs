using System.Windows;
using System.Windows.Controls;
using ImperialColors.Application.Configuration;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Cabeçalho do cupom não fiscal. O bloco da empresa tem que caber na MESMA faixa que o
/// corpo do cupom ocupa (as colunas de item somam 256), senão uma razão social longa quebra
/// só depois da borda: vaza na tela e sai cortada na impressora térmica, que imprime pela
/// largura do papel e não pela do visual.
/// </summary>
public class CupomCabecalhoTests
{
    private const double LarguraCorpoCupom = 256;

    /// <summary>Razão social real de cliente: longa o bastante para não caber numa linha.</summary>
    private const string RazaoSocialLonga =
        "Imperial Colors Comercio de Tintas Vernizes e Revestimentos Especiais LTDA ME";

    public CupomCabecalhoTests() => WpfTestBootstrap.Inicializar();

    private static CupomView CriarCupom(string razaoSocial)
    {
        var config = new Mock<IAppConfigService>();
        config.SetupGet(c => c.Empresa).Returns(new EmpresaConfig
        {
            NomeFantasia = "Imperial Colors",
            RazaoSocial = razaoSocial,
            CNPJ = "12.345.678/0001-99",
            InscricaoEstadual = "123.456.789.000",
            Endereco = "Av. Principal, 1000 - Curitiba - PR",
            Telefone = "(41) 3333-4444"
        });
        config.SetupGet(c => c.CupomRodape).Returns("Obrigado pela preferencia!");
        config.SetupGet(c => c.IconPath).Returns(string.Empty);

        var cupom = new CupomView(
            Mock.Of<IRelatorioService>(),
            config.Object,
            Mock.Of<ILocalConfigService>());

        cupom.InicializarVenda(new VendaDto
        {
            NumeroVenda = "000123",
            DataVenda = new DateTime(2026, 9, 25, 14, 30, 0),
            Total = 189.90m,
            Itens = [new ItemVendaDto { NomeProduto = "Tinta Acrilica Branca 18L", Quantidade = 1, PrecoUnitario = 189.90m }]
        });

        // Mesma medição que a impressão faz — sem isso as larguras ficam todas em zero.
        CupomPrintHelper.PrepararVisualParaImpressao((FrameworkElement)cupom.FindName("BorderCupom")!);
        return cupom;
    }

    [StaFact]
    public void RazaoSocialLonga_QuebraDentroDaLarguraDoCupom()
    {
        var cupom = CriarCupom(RazaoSocialLonga);
        var razaoSocial = (TextBlock)cupom.FindName("TxtEmpresaRazaoSocial")!;

        Assert.True(razaoSocial.ActualWidth <= LarguraCorpoCupom,
            $"a razão social ocupou {razaoSocial.ActualWidth:0} px, além dos {LarguraCorpoCupom} px do cupom");

        // Mais de uma linha: é o que prova que quebrou em vez de esticar.
        var alturaDeUmaLinha = razaoSocial.FontSize * razaoSocial.FontFamily.LineSpacing;
        Assert.True(razaoSocial.ActualHeight > alturaDeUmaLinha * 1.5,
            $"a razão social ficou com {razaoSocial.ActualHeight:0} px de altura — não quebrou linha");

        cupom.Close();
    }

    /// <summary>
    /// O cabeçalho é centralizado sobre a faixa do corpo do cupom, não sobre a borda inteira
    /// — é o alinhamento que existia antes, e que a correção da quebra precisava preservar.
    /// </summary>
    [StaFact]
    public void Cabecalho_FicaCentralizadoSobreAFaixaDoCorpoDoCupom()
    {
        var cupom = CriarCupom(RazaoSocialLonga);
        var cabecalho = (FrameworkElement)cupom.FindName("PainelCabecalhoCupom")!;

        Assert.Equal(LarguraCorpoCupom, cabecalho.ActualWidth);

        // Mesma origem à esquerda do corpo: os dois começam na borda do painel do cupom, então
        // compartilham o centro.
        var painel = (FrameworkElement)cupom.FindName("PainelCupom")!;
        var deslocamento = cabecalho.TransformToAncestor(painel).Transform(new Point(0, 0));
        Assert.Equal(0, deslocamento.X, 1);

        cupom.Close();
    }

    /// <summary>Razão social curta continua numa linha só — a correção não pode ter
    /// transformado o cabeçalho num bloco esticado.</summary>
    [StaFact]
    public void RazaoSocialCurta_ContinuaEmUmaLinhaSo()
    {
        var cupom = CriarCupom("Imperial Colors LTDA");
        var razaoSocial = (TextBlock)cupom.FindName("TxtEmpresaRazaoSocial")!;

        var alturaDeUmaLinha = razaoSocial.FontSize * razaoSocial.FontFamily.LineSpacing;
        Assert.True(razaoSocial.ActualHeight <= alturaDeUmaLinha * 1.5,
            $"a razão social curta ocupou {razaoSocial.ActualHeight:0} px — quebrou sem necessidade");

        cupom.Close();
    }
}
