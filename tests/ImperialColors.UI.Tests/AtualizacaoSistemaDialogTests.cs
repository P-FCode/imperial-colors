using System.Windows;
using System.Windows.Controls;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Construir a janela de verdade é o que valida o XAML inteiro de uma vez: recurso de tema
/// que não existe, nome de brush errado, handler de Click sem método correspondente. Nenhum
/// desses erros aparece em tempo de compilação — todos estouram na cara do operador, e nesta
/// tela especificamente eles apareceriam no pior momento, quando ele foi tentar atualizar.
/// </summary>
public class AtualizacaoSistemaDialogTests
{
    public AtualizacaoSistemaDialogTests() => WpfTestBootstrap.Inicializar();

    private static AtualizacaoSistemaDialogView CriarDialogo()
    {
        var atualizador = new Mock<IAtualizadorSistemaService>();
        atualizador.SetupGet(a => a.VersaoInstaladaTexto).Returns("v1.2.0");
        return new AtualizacaoSistemaDialogView(atualizador.Object);
    }

    [StaFact]
    public void Dialogo_ConstroiComOsControlesQueOFluxoUsa()
    {
        var dialogo = CriarDialogo();

        foreach (var nome in new[]
                 {
                     "PainelVersoes", "PainelNotas", "PainelMensagem", "PainelProgresso",
                     "TxtVersaoInstalada", "TxtVersaoNova", "TxtNotas", "TxtEtapa",
                     "BarraProgresso", "BtnAtualizar", "BtnCancelar"
                 })
        {
            Assert.True(dialogo.FindName(nome) is not null, $"'{nome}' não existe no XAML");
        }

        dialogo.Close();
    }

    /// <summary>
    /// Antes da consulta terminar não pode haver botão de atualizar: clicar em "Atualizar
    /// agora" sem saber ainda o que foi publicado não teria o que baixar.
    /// </summary>
    [StaFact]
    public void AoAbrir_BotaoAtualizarComecaEscondido()
    {
        var dialogo = CriarDialogo();

        var botao = (Button)dialogo.FindName("BtnAtualizar")!;
        var progresso = (StackPanel)dialogo.FindName("PainelProgresso")!;

        Assert.Equal(Visibility.Collapsed, botao.Visibility);
        Assert.Equal(Visibility.Collapsed, progresso.Visibility);

        dialogo.Close();
    }

    /// <summary>
    /// O ícone de erro usa LaranjaAvisoBrush, buscado por FindResource em tempo de execução —
    /// um nome errado ali só apareceria no exato momento em que a atualização falha, que é
    /// quando o operador menos precisa de um segundo erro por cima do primeiro.
    /// </summary>
    [StaFact]
    public void BrushesUsadosEmTempoDeExecucao_ExistemNoTema()
    {
        var dialogo = CriarDialogo();

        Assert.NotNull(dialogo.FindResource("LaranjaAvisoBrush"));
        Assert.NotNull(dialogo.FindResource("CinzaTextoBrush"));

        dialogo.Close();
    }
}
