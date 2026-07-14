using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Services;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class GestaoUsuariosViewModel : BaseViewModel
{
    private readonly IUsuarioService _usuarioService;
    private readonly ISessaoService _sessaoService;

    private ObservableCollection<UsuarioDto> _usuarios = new();
    public ObservableCollection<UsuarioDto> Usuarios { get => _usuarios; set => SetProperty(ref _usuarios, value); }

    private UsuarioDto? _usuarioSelecionado;
    public UsuarioDto? UsuarioSelecionado
    {
        get => _usuarioSelecionado;
        set
        {
            SetProperty(ref _usuarioSelecionado, value);
            OnPropertyChanged(nameof(TemSelecao));
            if (value is not null)
                PreencherFormulario(value);
        }
    }

    public bool TemSelecao => UsuarioSelecionado is not null;

    private string _nomeCompleto = string.Empty;
    public string NomeCompleto { get => _nomeCompleto; set => SetProperty(ref _nomeCompleto, value); }

    private string _username = string.Empty;
    public string Username { get => _username; set => SetProperty(ref _username, value); }

    private string _email = string.Empty;
    public string Email { get => _email; set => SetProperty(ref _email, value); }

    private string _senha = string.Empty;
    public string Senha { get => _senha; set => SetProperty(ref _senha, value); }

    private PermissaoUsuario _permissao = PermissaoUsuario.Caixa;
    public PermissaoUsuario Permissao { get => _permissao; set => SetProperty(ref _permissao, value); }

    private StatusUsuario _status = StatusUsuario.Aprovado;
    public StatusUsuario Status { get => _status; set => SetProperty(ref _status, value); }

    private bool _modoEdicao;
    public bool ModoEdicao
    {
        get => _modoEdicao;
        set
        {
            SetProperty(ref _modoEdicao, value);
            OnPropertyChanged(nameof(TituloFormulario));
            OnPropertyChanged(nameof(UsernameSomenteLeitura));
        }
    }

    public string TituloFormulario => ModoEdicao ? "Editar Usuário" : "Novo Usuário";
    public bool UsernameSomenteLeitura => ModoEdicao;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand SalvarCommand { get; }
    public RelayCommand NovoCommand { get; }
    public AsyncRelayCommand AprovarCommand { get; }
    public AsyncRelayCommand CancelarUsuarioCommand { get; }
    public AsyncRelayCommand ExcluirUsuarioCommand { get; }

    public GestaoUsuariosViewModel(IUsuarioService usuarioService, ISessaoService sessaoService)
    {
        _usuarioService = usuarioService;
        _sessaoService = sessaoService;
        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
        NovoCommand = new RelayCommand(NovoUsuario);
        AprovarCommand = new AsyncRelayCommand(AprovarAsync, () => TemSelecao);
        CancelarUsuarioCommand = new AsyncRelayCommand(CancelarUsuarioAsync, () => TemSelecao);
        ExcluirUsuarioCommand = new AsyncRelayCommand(ExcluirUsuarioAsync, () => TemSelecao);
    }

    public async Task CarregarAsync()
    {
        try
        {
            Carregando = true;
            var lista = await _usuarioService.ObterTodosAsync();
            Usuarios = new ObservableCollection<UsuarioDto>(lista);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar usuários: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private void NovoUsuario()
    {
        ModoEdicao = false;
        UsuarioSelecionado = null;
        NomeCompleto = string.Empty;
        Username = string.Empty;
        Email = string.Empty;
        Senha = string.Empty;
        Permissao = PermissaoUsuario.Caixa;
        Status = StatusUsuario.Aprovado;
    }

    private void PreencherFormulario(UsuarioDto usuario)
    {
        ModoEdicao = true;
        NomeCompleto = usuario.NomeCompleto;
        Username = usuario.Username;
        Email = usuario.Email;
        Permissao = usuario.Permissao;
        Status = usuario.Status;
        Senha = string.Empty;
    }

    private async Task SalvarAsync()
    {
        try
        {
            Carregando = true;

            if (ModoEdicao && UsuarioSelecionado is not null)
            {
                await _usuarioService.AtualizarPorAdminAsync(new AtualizarUsuarioAdminDto
                {
                    Id = UsuarioSelecionado.Id,
                    NomeCompleto = NomeCompleto,
                    Email = Email,
                    Permissao = Permissao,
                    Status = Status,
                    NovaSenha = string.IsNullOrWhiteSpace(Senha) ? null : Senha
                });
                MostrarSucesso("Usuário atualizado com sucesso!");
            }
            else
            {
                await _usuarioService.CriarPorAdminAsync(new CriarUsuarioAdminDto
                {
                    NomeCompleto = NomeCompleto,
                    Username = Username,
                    Email = Email,
                    Senha = Senha,
                    Permissao = Permissao
                });
                MostrarSucesso("Usuário criado com sucesso!");
                NovoUsuario();
            }

            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task AprovarAsync()
    {
        if (UsuarioSelecionado is null) return;
        try
        {
            await _usuarioService.AprovarAsync(UsuarioSelecionado.Id);
            MostrarSucesso("Usuário aprovado!");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
    }

    private async Task CancelarUsuarioAsync()
    {
        if (UsuarioSelecionado is null) return;
        if (!ConfirmarAcao($"Cancelar o usuário '{UsuarioSelecionado.Username}'?")) return;

        try
        {
            await _usuarioService.CancelarAsync(UsuarioSelecionado.Id);
            MostrarSucesso("Usuário cancelado.");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
    }

    private async Task ExcluirUsuarioAsync()
    {
        if (UsuarioSelecionado is null) return;

        var mensagem = $"Excluir permanentemente o usuário '{UsuarioSelecionado.Username}'?\n\n" +
                       "Esta ação não pode ser desfeita. O registro será removido do banco de dados.";
        if (!ConfirmarAcao(mensagem)) return;

        try
        {
            var usuarioLogadoId = _sessaoService.UsuarioAtual?.Id
                ?? throw new InvalidOperationException("Sessão inválida. Faça login novamente.");

            await _usuarioService.ExcluirFisicamenteAsync(UsuarioSelecionado.Id, usuarioLogadoId);
            MostrarSucesso("Usuário excluído permanentemente.");
            NovoUsuario();
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
    }
}
