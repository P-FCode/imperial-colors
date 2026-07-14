using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

public class UsuarioDto
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public StatusUsuario Status { get; set; }
    public PermissaoUsuario Permissao { get; set; }
    public DateTime DataCadastro { get; set; }

    public string StatusDescricao => Status switch
    {
        StatusUsuario.AguardandoAprovacao => "Aguardando Aprovação",
        StatusUsuario.Aprovado => "Aprovado",
        StatusUsuario.Cancelado => "Cancelado",
        _ => Status.ToString()
    };

    public string PermissaoDescricao => Permissao switch
    {
        PermissaoUsuario.Admin => "Administrador",
        PermissaoUsuario.Caixa => "Caixa",
        _ => Permissao.ToString()
    };
}

public class UsuarioSessaoDto
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public PermissaoUsuario Permissao { get; set; }

    public bool EhAdmin => Permissao == PermissaoUsuario.Admin;
}

public class LoginDto
{
    public string Username { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class CadastroUsuarioDto
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class CriarUsuarioAdminDto
{
    public string NomeCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public PermissaoUsuario Permissao { get; set; } = PermissaoUsuario.Caixa;
}

public class AtualizarUsuarioAdminDto
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public PermissaoUsuario Permissao { get; set; }
    public StatusUsuario Status { get; set; }
    public string? NovaSenha { get; set; }
}
