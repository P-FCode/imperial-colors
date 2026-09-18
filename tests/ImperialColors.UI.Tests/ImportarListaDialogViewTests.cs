using System.Windows.Controls;
using ImperialColors.UI.Views;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Diálogo que escolhe o formato antes de importar a lista de uma venda externa. Construir
/// a janela de verdade valida o XAML inteiro; o resto cobre o que o operador vê mudar: o
/// tutorial tem que acompanhar a opção marcada, senão ele monta a planilha seguindo a
/// instrução do formato errado.
/// </summary>
public class ImportarListaDialogViewTests
{
    public ImportarListaDialogViewTests() => WpfTestBootstrap.Inicializar();

    private static (ImportarListaDialogView Dialogo, TextBlock Instrucao, TextBlock Exemplo) Criar()
    {
        var dialogo = new ImportarListaDialogView();
        return (dialogo,
            (TextBlock)dialogo.FindName("TxtComoMontar")!,
            (TextBlock)dialogo.FindName("TxtExemplo")!);
    }

    [StaFact]
    public void AoAbrir_JaMostraOTutorialDoExcelComOBotaoImportarBloqueado()
    {
        var (dialogo, instrucao, exemplo) = Criar();

        // Excel é o padrão: é o formato que o operador tem na mão depois de montar a lista
        // da rua, e o que o pedido pediu para passar a aceitar.
        Assert.True(((RadioButton)dialogo.FindName("RbXlsx")!).IsChecked);
        Assert.Contains("primeira aba", instrucao.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quantidade", exemplo.Text, StringComparison.OrdinalIgnoreCase);

        // Sem arquivo escolhido não há o que importar.
        Assert.False(((Button)dialogo.FindName("BtnImportar")!).IsEnabled);
        Assert.Null(dialogo.CaminhoArquivo);
    }

    [StaFact]
    public void AoTrocarParaCsv_OTutorialEOExemploAcompanham()
    {
        var (dialogo, instrucao, exemplo) = Criar();

        ((RadioButton)dialogo.FindName("RbCsv")!).IsChecked = true;

        Assert.Contains("ponto e vírgula", instrucao.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aspas", instrucao.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7891234567890;Tinta Branca 18L;2", exemplo.Text);
    }

    [StaFact]
    public void AoTrocarParaTxt_OTutorialMostraOFormatoOriginalEOComentario()
    {
        var (dialogo, instrucao, exemplo) = Criar();

        ((RadioButton)dialogo.FindName("RbTxt")!).IsChecked = true;

        Assert.Contains("CODIGO_DE_BARRAS;NOME_DO_PRODUTO;QUANTIDADE", instrucao.Text);
        Assert.StartsWith("#", exemplo.Text, StringComparison.Ordinal);
    }
}
