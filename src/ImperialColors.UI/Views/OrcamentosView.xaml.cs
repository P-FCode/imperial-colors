using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class OrcamentosView : UserControl
{
    private readonly OrcamentoViewModel _viewModel;

    public OrcamentosView(OrcamentoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _viewModel.ExecutarEdicaoSeSelecionado();
}
