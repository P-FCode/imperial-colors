using ImperialColors.Domain.Enums;
using ImperialColors.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class GestaoUsuariosView : UserControl
{
    private readonly GestaoUsuariosViewModel _viewModel;

    public GestaoUsuariosView(GestaoUsuariosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        CmbPermissao.ItemsSource = Enum.GetValues<PermissaoUsuario>();
        CmbStatus.ItemsSource = Enum.GetValues<StatusUsuario>();

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(GestaoUsuariosViewModel.Permissao))
                CmbPermissao.SelectedItem = _viewModel.Permissao;
            if (e.PropertyName is nameof(GestaoUsuariosViewModel.Status))
                CmbStatus.SelectedItem = _viewModel.Status;
        };

        Loaded += async (_, _) => await _viewModel.CarregarAsync();
    }

    private void CmbPermissao_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbPermissao.SelectedItem is PermissaoUsuario p)
            _viewModel.Permissao = p;
    }

    private void CmbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbStatus.SelectedItem is StatusUsuario s)
            _viewModel.Status = s;
    }
}
