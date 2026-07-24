using System.Windows.Controls;
using System.Windows.Input;
using ImperialColors.UI.ViewModels;

namespace ImperialColors.UI.Views;

public partial class AuditoriaLogsView : UserControl
{
    public AuditoriaLogsView(AuditoriaLogsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.CarregarAsync();
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is AuditoriaLogsViewModel vm && vm.VerDetalhesCommand.CanExecute(null))
            vm.VerDetalhesCommand.Execute(null);
    }
}
