using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;

namespace ImperialColors.UI.Services;

public interface ISessaoService
{
    UsuarioSessaoDto? UsuarioAtual { get; }
    bool EstaAutenticado { get; }
    bool EhAdmin { get; }
    bool EhCaixa { get; }

    void IniciarSessao(UsuarioSessaoDto usuario);
    void EncerrarSessao();
    string ObterNomeUsuario();
}

public class SessaoService : ISessaoService
{
    private UsuarioSessaoDto? _usuarioAtual;

    public UsuarioSessaoDto? UsuarioAtual => _usuarioAtual;
    public bool EstaAutenticado => _usuarioAtual is not null;
    public bool EhAdmin => _usuarioAtual?.Permissao == PermissaoUsuario.Admin;
    public bool EhCaixa => _usuarioAtual?.Permissao == PermissaoUsuario.Caixa;

    public void IniciarSessao(UsuarioSessaoDto usuario)
    {
        _usuarioAtual = usuario;
    }

    public void EncerrarSessao()
    {
        _usuarioAtual = null;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
    }

    public string ObterNomeUsuario()
        => _usuarioAtual?.Username ?? "sistema";
}
