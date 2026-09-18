using System.Windows;
using System.Windows.Controls;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Views;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Diálogo que escolhe NF-e ou NFC-e ao faturar uma venda. Construir a janela de verdade
/// valida o XAML inteiro — StaticResource de tema que não resolve e x:Name que sumiu não
/// aparecem em tempo de compilação, só estourariam no clique do operador.
/// </summary>
public class SelecionarTipoNotaDialogViewTests
{
    public SelecionarTipoNotaDialogViewTests() => WpfTestBootstrap.Inicializar();

    private static VendaDto CriarVenda() => new()
    {
        Id = 7,
        NumeroVenda = "000123",
        Total = 350m,
        Status = StatusVenda.Finalizada,
        NomeCompradorCupom = "João Comprador",
        Itens = [new ItemVendaDto(), new ItemVendaDto()]
    };

    [StaFact]
    public void Dialogo_ConstroiComOsDoisCaminhosDeEmissao()
    {
        var dialogo = new SelecionarTipoNotaDialogView(CriarVenda());

        Assert.NotNull(dialogo.FindName("BtnNFe"));
        Assert.NotNull(dialogo.FindName("BtnNFCe"));
        Assert.Null(dialogo.TipoSelecionado); // nada escolhido antes do clique
        Assert.Contains("000123", ((TextBlock)dialogo.FindName("TxtTitulo")!).Text);
    }

    /// <summary>Sem nota anterior o aviso amarelo fica fora da tela — ele só existe para o
    /// caso de já haver rascunho/rejeitada esperando na lista de notas.</summary>
    [StaFact]
    public void SemNotaAnterior_NaoMostraOAviso()
    {
        var dialogo = new SelecionarTipoNotaDialogView(CriarVenda());

        Assert.Equal(Visibility.Collapsed, ((Border)dialogo.FindName("PainelAviso")!).Visibility);
    }

    [StaFact]
    public void ComRascunhoAnterior_AvisaQueUmNovoRascunhoSeraCriado()
    {
        var dialogo = new SelecionarTipoNotaDialogView(
            CriarVenda(),
            [
                new NotaFiscalResumoDto
                {
                    Id = 9,
                    Tipo = TipoNotaFiscal.NFe,
                    Serie = "1",
                    Numero = "10",
                    Status = StatusNotaFiscal.Rascunho
                }
            ]);

        Assert.Equal(Visibility.Visible, ((Border)dialogo.FindName("PainelAviso")!).Visibility);
        Assert.Contains("NF-e 1/10", ((TextBlock)dialogo.FindName("TxtAviso")!).Text);
    }
}
