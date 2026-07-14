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
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUsuarioRepository usuarioRepository, ILogger<AuthService> logger)
    {
        _usuarioRepository = usuarioRepository;
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
            throw new DomainException("Usuário ou senha inválidos.");

        if (usuario.Status == StatusUsuario.AguardandoAprovacao)
            throw new DomainException("Sua conta está aguardando aprovação do administrador.");

        if (usuario.Status == StatusUsuario.Cancelado)
            throw new DomainException("Sua conta foi cancelada. Entre em contato com o administrador.");

        if (!PasswordHasher.Verificar(dto.Senha, usuario.SenhaHash, usuario.Salt))
        {
            _logger.LogWarning("Tentativa de login inválida para usuário {Username}", usuario.Username);
            throw new DomainException("Usuário ou senha inválidos.");
        }

        _logger.LogInformation("Login bem-sucedido: {Username}", usuario.Username);

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
            DataCadastro = DateTime.UtcNow
        };

        var criado = await _usuarioRepository.AdicionarAsync(usuario);
        _logger.LogInformation("Novo cadastro aguardando aprovação: {Username}", username);

        return MapParaDto(criado);
    }

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
