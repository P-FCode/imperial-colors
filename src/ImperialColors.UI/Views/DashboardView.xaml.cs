using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.ViewModels;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel, IAppConfigService config)
    {
        InitializeComponent();
        DataContext = viewModel;
        TxtEmpresaDashboard.Text = $"{config.EmpresaNome} — {config.EmpresaSubtitulo}";
        LogoHelper.AplicarLogo(ImgLogoDashboard, config.LogoSemFundoPath, 48, 48);
    }
}
