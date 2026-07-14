using ImperialColors.Application.DTOs;
using ImperialColors.Application.Security;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _repository = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_repository.Object, NullLogger<AuthService>.Instance);
    }

    private static Usuario CriarUsuarioAprovado(string username, string senha, string email)
    {
        var (hash, salt) = PasswordHasher.HashPassword(senha);
        return new Usuario
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Usuário Teste",
            Username = username,
            Email = email,
            SenhaHash = hash,
            Salt = salt,
            Status = StatusUsuario.Aprovado,
            Permissao = PermissaoUsuario.Admin
        };
    }

    [Fact]
    public async Task LoginAsync_ComCredenciaisValidas_RetornaSessao()
    {
        var usuario = CriarUsuarioAprovado("admin", "Admin@1234", "admin@test.local");
        _repository.Setup(r => r.ObterPorUsernameAsync("admin")).ReturnsAsync(usuario);

        var sessao = await _authService.LoginAsync(new LoginDto
        {
            Username = "admin",
            Senha = "Admin@1234"
        });

        Assert.Equal("admin", sessao.Username);
        Assert.Equal(PermissaoUsuario.Admin, sessao.Permissao);
    }

    [Fact]
    public async Task LoginAsync_ComEmailValido_RetornaSessao()
    {
        var usuario = CriarUsuarioAprovado("teste", "Teste@1234", "teste@gmail.com");
        _repository.Setup(r => r.ObterPorEmailAsync("teste@gmail.com")).ReturnsAsync(usuario);

        var sessao = await _authService.LoginAsync(new LoginDto
        {
            Username = "teste@gmail.com",
            Senha = "Teste@1234"
        });

        Assert.Equal("teste", sessao.Username);
    }

    [Fact]
    public async Task LoginAsync_ComSenhaInvalida_LancaDomainException()
    {
        var usuario = CriarUsuarioAprovado("admin", "Admin@1234", "admin@test.local");
        _repository.Setup(r => r.ObterPorUsernameAsync("admin")).ReturnsAsync(usuario);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _authService.LoginAsync(new LoginDto { Username = "admin", Senha = "errada" }));

        Assert.Equal("Usuário ou senha inválidos.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ComContaAguardandoAprovacao_LancaDomainException()
    {
        var usuario = CriarUsuarioAprovado("teste", "Teste@1234", "teste@test.local");
        usuario.Status = StatusUsuario.AguardandoAprovacao;
        _repository.Setup(r => r.ObterPorUsernameAsync("teste")).ReturnsAsync(usuario);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _authService.LoginAsync(new LoginDto { Username = "teste", Senha = "Teste@1234" }));

        Assert.Contains("aguardando aprovação", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_ComCamposVazios_LancaDomainException()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _authService.LoginAsync(new LoginDto { Username = "", Senha = "" }));

        Assert.Equal("Usuário e senha são obrigatórios.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ComUsuarioInexistente_LancaDomainException()
    {
        _repository.Setup(r => r.ObterPorUsernameAsync("inexistente")).ReturnsAsync((Usuario?)null);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            _authService.LoginAsync(new LoginDto { Username = "inexistente", Senha = "Admin@1234" }));

        Assert.Equal("Usuário ou senha inválidos.", ex.Message);
    }
}
