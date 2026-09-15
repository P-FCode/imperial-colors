using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
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

/// <summary>
/// AUDITORIA (15/09, segunda rodada) — usuários e login. Caixa-branca com Moq, sem banco.
/// </summary>
public class Auditoria2UsuarioTests
{
    private static Usuario Admin(string senha = "Senha123")
    {
        var (hash, salt) = PasswordHasher.HashPassword(senha);
        return new Usuario
        {
            Id = Guid.NewGuid(),
            NomeCompleto = "Único Admin",
            Username = "admin",
            Email = "admin@loja.com",
            SenhaHash = hash,
            Salt = salt,
            Status = StatusUsuario.Aprovado,
            Permissao = PermissaoUsuario.Admin
        };
    }

    private static Mock<IUsuarioRepository> RepositorioComUnicoAdmin(Usuario admin)
    {
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ObterPorIdAsync(admin.Id)).ReturnsAsync(admin);
        repo.Setup(r => r.EmailExisteAsync(It.IsAny<string>(), It.IsAny<Guid?>())).ReturnsAsync(false);
        repo.Setup(r => r.ContarAdminsAprovadosAsync(It.IsAny<Guid?>())).ReturnsAsync(1);
        repo.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);
        return repo;
    }

    /// <summary>
    /// ExcluirFisicamenteAsync protege o último administrador, mas AtualizarPorAdminAsync não:
    /// basta editar o único admin e trocar a permissão para Caixa. Ninguém mais consegue
    /// abrir Gestão de Usuários — a loja fica sem administrador e sem tela para voltar atrás.
    /// </summary>
    [Fact]
    public async Task RebaixarOUnicoAdministrador_DeveSerBloqueado()
    {
        var admin = Admin();
        var repo = RepositorioComUnicoAdmin(admin);
        var servico = new UsuarioService(repo.Object, Mock.Of<IAuditoriaService>(), new UsuarioAtualSistema(), NullLogger<UsuarioService>.Instance);

        var erro = await Record.ExceptionAsync(() => servico.AtualizarPorAdminAsync(new AtualizarUsuarioAdminDto
        {
            Id = admin.Id,
            NomeCompleto = admin.NomeCompleto,
            Email = admin.Email,
            Permissao = PermissaoUsuario.Caixa,
            Status = StatusUsuario.Aprovado
        }));

        Assert.True(erro is DomainException, "o único administrador foi rebaixado para Caixa sem bloqueio.");
    }

    /// <summary>Mesma lacuna pelo botão "Cancelar usuário".</summary>
    [Fact]
    public async Task CancelarOUnicoAdministrador_DeveSerBloqueado()
    {
        var admin = Admin();
        var repo = RepositorioComUnicoAdmin(admin);
        var servico = new UsuarioService(repo.Object, Mock.Of<IAuditoriaService>(), new UsuarioAtualSistema(), NullLogger<UsuarioService>.Instance);

        var erro = await Record.ExceptionAsync(() => servico.CancelarAsync(admin.Id));

        Assert.True(erro is DomainException, "o único administrador foi cancelado sem bloqueio.");
    }

    /// <summary>
    /// O status da conta é checado ANTES da senha. Com qualquer senha errada, o sistema
    /// confirma que o usuário existe e diz em que situação a conta está.
    /// </summary>
    [Fact]
    public async Task LoginComSenhaErrada_NaoDeveRevelarSituacaoDaConta()
    {
        var usuario = Admin();
        usuario.Status = StatusUsuario.Cancelado;

        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ObterPorUsernameAsync("admin")).ReturnsAsync(usuario);
        var auth = new AuthService(repo.Object, Mock.Of<IAuditoriaService>(), NullLogger<AuthService>.Instance);

        var erro = await Assert.ThrowsAsync<DomainException>(
            () => auth.LoginAsync(new LoginDto { Username = "admin", Senha = "senha-totalmente-errada" }));

        Assert.Equal("Usuário ou senha inválidos.", erro.Message);
    }

    /// <summary>
    /// Caracterização (PASSA hoje): não há bloqueio nem atraso após tentativas erradas.
    /// Documenta a ausência de proteção contra força bruta na tela de login.
    /// </summary>
    [Fact]
    public async Task LoginAposDezTentativasErradas_AindaAceitaASenhaCerta()
    {
        var usuario = Admin("Senha123");
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ObterPorUsernameAsync("admin")).ReturnsAsync(usuario);
        var auth = new AuthService(repo.Object, Mock.Of<IAuditoriaService>(), NullLogger<AuthService>.Instance);

        for (var i = 0; i < 10; i++)
            await Assert.ThrowsAsync<DomainException>(
                () => auth.LoginAsync(new LoginDto { Username = "admin", Senha = $"Errada{i}" }));

        var sessao = await auth.LoginAsync(new LoginDto { Username = "admin", Senha = "Senha123" });
        Assert.Equal(usuario.Id, sessao.Id);
    }
}
