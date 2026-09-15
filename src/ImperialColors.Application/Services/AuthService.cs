using ImperialColors.Domain.Helpers;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class AuthService : IAuthService
{
    private const string ModuloAuditoria = "Usuários";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUsuarioRepository usuarioRepository, IAuditoriaService auditoria, ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<UsuarioSessaoDto> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Senha))
            throw new DomainException("Usuário e senha são obrigatórios.");

        var identificador = dto.Username.Trim();
        Usuario? usuario;

        if (identificador.Contains('@'))
        {
            var email = InputSanitizer.SanitizarEmail(identificador);
            usuario = await _usuarioRepository.ObterPorEmailAsync(email);
        }
        else
        {
            var username = InputSanitizer.SanitizarUsername(identificador);
            if (string.IsNullOrWhiteSpace(username))
                throw new DomainException("Usuário ou senha inválidos.");

            usuario = await _usuarioRepository.ObterPorUsernameAsync(username);
        }

        if (usuario is null)
        {
            await RegistrarLoginAsync("LOGIN_FALHA", "Sistema",
                $"Tentativa de login com usuário inexistente '{InputSanitizer.SanitizarTexto(identificador, 60)}'", NivelLogAuditoria.Warning);
            throw new DomainException("Usuário ou senha inválidos.");
        }

        if (usuario.Status == StatusUsuario.AguardandoAprovacao)
        {
            await RegistrarLoginAsync("LOGIN_FALHA", usuario.NomeCompleto,
                $"Login recusado para '{usuario.Username}': conta aguardando aprovação", NivelLogAuditoria.Warning);
            throw new DomainException("Sua conta está aguardando aprovação do administrador.");
        }

        if (usuario.Status == StatusUsuario.Cancelado)
        {
            await RegistrarLoginAsync("LOGIN_FALHA", usuario.NomeCompleto,
                $"Login recusado para '{usuario.Username}': conta cancelada", NivelLogAuditoria.Warning);
            throw new DomainException("Sua conta foi cancelada. Entre em contato com o administrador.");
        }

        if (!PasswordHasher.Verificar(dto.Senha, usuario.SenhaHash, usuario.Salt))
        {
            _logger.LogWarning("Tentativa de login inválida para usuário {Username}", usuario.Username);
            await RegistrarLoginAsync("LOGIN_FALHA", usuario.NomeCompleto,
                $"Senha incorreta para '{usuario.Username}'", NivelLogAuditoria.Warning);
            throw new DomainException("Usuário ou senha inválidos.");
        }

        _logger.LogInformation("Login bem-sucedido: {Username}", usuario.Username);
        await RegistrarLoginAsync("LOGIN_SUCESSO", usuario.NomeCompleto,
            $"Login de '{usuario.Username}' ({usuario.Permissao})", NivelLogAuditoria.Info);

        return new UsuarioSessaoDto
        {
            Id = usuario.Id,
            NomeCompleto = usuario.NomeCompleto,
            Username = usuario.Username,
            Permissao = usuario.Permissao
        };
    }

    public async Task<UsuarioDto> RegistrarAsync(CadastroUsuarioDto dto)
    {
        var nome = InputSanitizer.SanitizarTexto(dto.NomeCompleto, 200);
        var username = InputSanitizer.SanitizarUsername(dto.Username);
        var email = InputSanitizer.SanitizarEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome completo é obrigatório.");
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            throw new DomainException("Nome de usuário deve ter pelo menos 3 caracteres (letras, números, . _ -).");
        if (!InputSanitizer.EmailValido(email))
            throw new DomainException("E-mail inválido.");
        if (!InputSanitizer.SenhaForte(dto.Senha))
            throw new DomainException("Senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número.");
        if (dto.Senha != dto.ConfirmarSenha)
            throw new DomainException("As senhas não coincidem.");

        if (await _usuarioRepository.UsernameExisteAsync(username))
            throw new DomainException("Nome de usuário já está em uso.");
        if (await _usuarioRepository.EmailExisteAsync(email))
            throw new DomainException("E-mail já está cadastrado.");

        var (hash, salt) = PasswordHasher.HashPassword(dto.Senha);

        var usuario = new Usuario
        {
            NomeCompleto = nome,
            Username = username,
            Email = email,
            SenhaHash = hash,
            Salt = salt,
            Status = StatusUsuario.AguardandoAprovacao,
            Permissao = PermissaoUsuario.Caixa,
            DataCadastro = Relogio.Agora
        };

        var criado = await _usuarioRepository.AdicionarAsync(usuario);
        _logger.LogInformation("Novo cadastro aguardando aprovação: {Username}", username);

        await RegistrarLoginAsync("USUARIO_CADASTRO_SOLICITADO", criado.NomeCompleto,
            $"Cadastro de '{criado.Username}' solicitado pela tela de login — aguardando aprovação", NivelLogAuditoria.Info);

        return MapParaDto(criado);
    }

    private Task RegistrarLoginAsync(string acao, string nomeUsuario, string descricao, NivelLogAuditoria nivel)
        => _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = nomeUsuario,
            Modulo = ModuloAuditoria,
            Acao = acao,
            Descricao = descricao,
            Nivel = nivel
        });

    private static UsuarioDto MapParaDto(Usuario u) => new()
    {
        Id = u.Id,
        NomeCompleto = u.NomeCompleto,
        Username = u.Username,
        Email = u.Email,
        Status = u.Status,
        Permissao = u.Permissao,
        DataCadastro = u.DataCadastro
    };
}
