using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(IUsuarioRepository usuarioRepository, ILogger<UsuarioService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<UsuarioDto>> ObterTodosAsync()
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync();
        return usuarios.Select(MapParaDto);
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id);
        return usuario is null ? null : MapParaDto(usuario);
    }

    public async Task<UsuarioDto> CriarPorAdminAsync(CriarUsuarioAdminDto dto)
    {
        var nome = InputSanitizer.SanitizarTexto(dto.NomeCompleto, 200);
        var username = InputSanitizer.SanitizarUsername(dto.Username);
        var email = InputSanitizer.SanitizarEmail(dto.Email);

        ValidarDadosBasicos(nome, username, email, dto.Senha);

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
            Status = StatusUsuario.Aprovado,
            Permissao = dto.Permissao,
            DataCadastro = DateTime.UtcNow
        };

        var criado = await _usuarioRepository.AdicionarAsync(usuario);
        _logger.LogInformation("Usuário criado pelo admin: {Username} ({Permissao})", username, dto.Permissao);
        return MapParaDto(criado);
    }

    public async Task<UsuarioDto> AtualizarPorAdminAsync(AtualizarUsuarioAdminDto dto)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(dto.Id)
            ?? throw new DomainException("Usuário não encontrado.");

        var nome = InputSanitizer.SanitizarTexto(dto.NomeCompleto, 200);
        var email = InputSanitizer.SanitizarEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome completo é obrigatório.");
        if (!InputSanitizer.EmailValido(email))
            throw new DomainException("E-mail inválido.");

        if (await _usuarioRepository.EmailExisteAsync(email, dto.Id))
            throw new DomainException("E-mail já está em uso por outro usuário.");

        usuario.NomeCompleto = nome;
        usuario.Email = email;
        usuario.Permissao = dto.Permissao;
        usuario.Status = dto.Status;

        if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
        {
            if (!InputSanitizer.SenhaForte(dto.NovaSenha))
                throw new DomainException("Nova senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número.");

            var (hash, salt) = PasswordHasher.HashPassword(dto.NovaSenha);
            usuario.SenhaHash = hash;
            usuario.Salt = salt;
        }

        var atualizado = await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário atualizado: {Username}", usuario.Username);
        return MapParaDto(atualizado);
    }

    public async Task CancelarAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        usuario.Status = StatusUsuario.Cancelado;
        await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário cancelado: {Username}", usuario.Username);
    }

    public async Task AprovarAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        usuario.Status = StatusUsuario.Aprovado;
        await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário aprovado: {Username}", usuario.Username);
    }

    public async Task ExcluirFisicamenteAsync(Guid id, Guid usuarioLogadoId)
    {
        if (id == usuarioLogadoId)
            throw new DomainException("Você não pode excluir o usuário da sessão ativa.");

        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        if (usuario.Permissao == PermissaoUsuario.Admin &&
            usuario.Status == StatusUsuario.Aprovado &&
            await _usuarioRepository.ContarAdminsAprovadosAsync() <= 1)
        {
            throw new DomainException(
                "Não é possível excluir o único administrador aprovado do sistema. Cadastre outro administrador antes.");
        }

        await _usuarioRepository.RemoverFisicamenteAsync(id);
        _logger.LogInformation("Usuário excluído permanentemente: {Username}", usuario.Username);
    }

    private static void ValidarDadosBasicos(string nome, string username, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome completo é obrigatório.");
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            throw new DomainException("Nome de usuário deve ter pelo menos 3 caracteres.");
        if (!InputSanitizer.EmailValido(email))
            throw new DomainException("E-mail inválido.");
        if (!InputSanitizer.SenhaForte(senha))
            throw new DomainException("Senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número.");
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
