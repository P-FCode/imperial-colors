using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.ViewModels;
using System.Windows;

namespace ImperialColors.UI.Views;

public partial class LoginView : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel, IAppConfigService config)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        TxtEmpresaNome.Text = config.EmpresaNome;
        TxtEmpresaSubtitulo.Text = config.EmpresaSubtitulo;
        LogoHelper.AplicarLogo(ImgLogo, config.LogoSemFundoPath, 72, 72);
        LogoHelper.AplicarIconeJanela(this, config.IconPath);

        _viewModel.LoginSucesso += () =>
        {
            DialogResult = true;
            Close();
        };

        Closing += (_, e) =>
        {
            if (DialogResult is null)
                DialogResult = false;
        };
    }

    private void SincronizarSenhaLogin()
        => _viewModel.SenhaLogin = TxtSenhaLogin.Password;

    private void BtnEntrar_Click(object sender, RoutedEventArgs e)
        => SincronizarSenhaLogin();

    private void TxtSenhaLogin_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            SincronizarSenhaLogin();
            if (_viewModel.LoginCommand.CanExecute(null))
                _viewModel.LoginCommand.Execute(null);
        }
    }

    private void TxtSenhaLogin_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.SenhaLogin = TxtSenhaLogin.Password;

    private void TxtSenhaCadastro_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.SenhaCadastro = TxtSenhaCadastro.Password;

    private void TxtConfirmarSenha_PasswordChanged(object sender, RoutedEventArgs e)
        => _viewModel.ConfirmarSenha = TxtConfirmarSenha.Password;
}
