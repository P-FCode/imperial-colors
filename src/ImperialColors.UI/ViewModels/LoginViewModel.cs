using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;

namespace ImperialColors.UI.ViewModels;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISessaoService _sessaoService;

    private string _username = string.Empty;
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private string _senhaLogin = string.Empty;
    public string SenhaLogin { get => _senhaLogin; set => SetProperty(ref _senhaLogin, value); }

    private string _nomeCompleto = string.Empty;
    public string NomeCompleto { get => _nomeCompleto; set => SetProperty(ref _nomeCompleto, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _senhaCadastro = string.Empty;
    public string SenhaCadastro { get => _senhaCadastro; set => SetProperty(ref _senhaCadastro, value); }

    private string _confirmarSenha = string.Empty;
    public string ConfirmarSenha { get => _confirmarSenha; set => SetProperty(ref _confirmarSenha, value); }

    public AsyncRelayCommand LoginCommand { get; }
    public AsyncRelayCommand RegistrarCommand { get; }

    public event Action? LoginSucesso;

    public LoginViewModel(IAuthService authService, ISessaoService sessaoService)
    {
        _authService = authService;
        _sessaoService = sessaoService;
        LoginCommand = new AsyncRelayCommand(ExecutarLoginAsync);
        RegistrarCommand = new AsyncRelayCommand(ExecutarRegistroAsync);
    }

    private async Task ExecutarLoginAsync()
    {
        try
        {
            Carregando = true;
            MensagemErro = string.Empty;

            var sessao = await _authService.LoginAsync(new LoginDto
            {
                Username = Username,
                Senha = SenhaLogin
            });

            _sessaoService.IniciarSessao(sessao);

            await UiDispatcher.ExecutarNaUiAsync(async () =>
            {
                SenhaLogin = string.Empty;
                LoginSucesso?.Invoke();
                await Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
        finally
        {
            UiDispatcher.ExecutarNaUi(() => Carregando = false);
        }
    }

    private async Task ExecutarRegistroAsync()
    {
        try
        {
            Carregando = true;
            MensagemErro = string.Empty;

            await _authService.RegistrarAsync(new CadastroUsuarioDto
            {
                NomeCompleto = NomeCompleto,
                Username = Username,
                Email = Email,
                Senha = SenhaCadastro,
                ConfirmarSenha = ConfirmarSenha
            });

            MostrarSucesso("Cadastro realizado! Sua conta está aguardando aprovação do administrador.");
            LimparCadastro();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
        finally
        {
            UiDispatcher.ExecutarNaUi(() => Carregando = false);
        }
    }

    private void LimparCadastro()
    {
        NomeCompleto = string.Empty;
        Email = string.Empty;
        SenhaCadastro = string.Empty;
        ConfirmarSenha = string.Empty;
    }
}
