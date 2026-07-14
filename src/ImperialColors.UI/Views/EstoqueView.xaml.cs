using ImperialColors.UI.Helpers;
using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class EstoqueView : UserControl
{
    private readonly ProdutoViewModel _viewModel;

    public EstoqueView(ProdutoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _viewModel.ExecutarEdicaoSeSelecionado();

    private async void TxtBuscaEstoque_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        var termo = TxtBuscaEstoque.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
            return;

        if (!PareceCodigoBarras(termo))
            return;

        var encontrado = await _viewModel.ProcessarLeituraCodigoBarrasAsync(termo);
        if (encontrado)
            return;

        _viewModel.TermoBusca = string.Empty;
        BarcodeScanHelper.ProdutoNaoEncontrado(TxtBuscaEstoque, TxtAvisoBarrasEstoque);
    }

    private static bool PareceCodigoBarras(string termo)
        => termo.Length >= 4 && termo.All(char.IsDigit);
}
