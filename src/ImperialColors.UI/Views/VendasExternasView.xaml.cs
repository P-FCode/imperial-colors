using ImperialColors.UI.ViewModels;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class VendasExternasView : UserControl
{
    public VendasExternasView(VendaExternaViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
