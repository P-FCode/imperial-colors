using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class ClientesView : UserControl
{
    private readonly ClienteViewModel _viewModel;

    public ClientesView(ClienteViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _viewModel.ExecutarEdicaoSeSelecionado();
}
