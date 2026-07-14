using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class VendasView : UserControl
{
    private readonly VendaViewModel _viewModel;

    public VendasView(VendaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.VendaSelecionada is not null)
            _viewModel.VisualizarVendaCommand.Execute(null);
    }
}
