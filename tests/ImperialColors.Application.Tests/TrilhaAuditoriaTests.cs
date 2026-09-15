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
/// A2 da auditoria de 15/09: ações sensíveis precisam deixar registro em logs_auditoria com o
/// autor. Caixa-branca com Moq, sem banco.
/// </summary>
public class TrilhaAuditoriaTests
{
    private sealed class OperadorFixo(string nome) : IUsuarioAtual
    {
        public string Nome { get; } = nome;
    }

    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly List<RegistrarLogAuditoriaDto> _registros = [];

    public TrilhaAuditoriaTests()
    {
        _auditoria
            .Setup(a => a.RegistrarAsync(It.IsAny<RegistrarLogAuditoriaDto>(), It.IsAny<CancellationToken>()))
            .Callback<RegistrarLogAuditoriaDto, CancellationToken>((dto, _) => _registros.Add(dto))
            .Returns(Task.CompletedTask);
    }

    private static Usuario UsuarioAprovado(string senha, PermissaoUsuario permissao = PermissaoUsuario.Caixa)
    {
        var (hash, salt) = PasswordHasher.HashPassword(senha);
        return new Usuario
        {
            Id = Guid.NewGuid(), NomeCompleto = "Maria Caixa", Username = "maria", Email = "maria@loja.com",
            SenhaHash = hash, Salt = salt, Status = StatusUsuario.Aprovado, Permissao = permissao
        };
    }

    [Fact]
    public async Task Login_RegistraFalhaESucesso()
    {
        var usuario = UsuarioAprovado("Senha123");
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ObterPorUsernameAsync("maria")).ReturnsAsync(usuario);
        var auth = new AuthService(repo.Object, _auditoria.Object, NullLogger<AuthService>.Instance);

        await Assert.ThrowsAsync<DomainException>(() => auth.LoginAsync(new LoginDto { Username = "maria", Senha = "Errada123" }));
        await Assert.ThrowsAsync<DomainException>(() => auth.LoginAsync(new LoginDto { Username = "fantasma", Senha = "Errada123" }));
        await auth.LoginAsync(new LoginDto { Username = "maria", Senha = "Senha123" });

        Assert.Collection(_registros,
            r => { Assert.Equal("LOGIN_FALHA", r.Acao); Assert.Contains("Senha incorreta", r.Descricao); },
            r => { Assert.Equal("LOGIN_FALHA", r.Acao); Assert.Contains("inexistente", r.Descricao); },
            r => { Assert.Equal("LOGIN_SUCESSO", r.Acao); Assert.Equal("Maria Caixa", r.NomeUsuario); });
    }

    [Fact]
    public async Task AlterarPermissaoDeUsuario_RegistraAutorEMudanca()
    {
        var usuario = UsuarioAprovado("Senha123");
        var repo = new Mock<IUsuarioRepository>();
        repo.Setup(r => r.ObterPorIdAsync(usuario.Id)).ReturnsAsync(usuario);
        repo.Setup(r => r.AtualizarAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);
        var servico = new UsuarioService(repo.Object, _auditoria.Object, new OperadorFixo("Dono da Loja"), NullLogger<UsuarioService>.Instance);

        await servico.AtualizarPorAdminAsync(new AtualizarUsuarioAdminDto
        {
            Id = usuario.Id, NomeCompleto = usuario.NomeCompleto, Email = usuario.Email,
            Permissao = PermissaoUsuario.Admin, Status = StatusUsuario.Aprovado
        });

        var registro = Assert.Single(_registros);
        Assert.Equal("USUARIO_ALTERADO", registro.Acao);
        Assert.Equal("Dono da Loja", registro.NomeUsuario);
        Assert.Contains("permissão Caixa → Admin", registro.Descricao);
    }

    [Fact]
    public async Task MovimentacaoManualDeEstoque_RegistraAntesEDepois()
    {
        var produtoRepo = new Mock<IProdutoRepository>();
        produtoRepo.Setup(r => r.AjustarEstoqueTransacionalAsync(7, TipoMovimentacao.Saida, 2m, "Quebra", "maria", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovimentacaoEstoque { ProdutoId = 7, QuantidadeAnterior = 10m, QuantidadeAtual = 8m });
        produtoRepo.Setup(r => r.ObterPorIdAsync(7)).ReturnsAsync(new Produto { Id = 7, Nome = "Tinta Acrílica 18L" });

        var servico = new ProdutoService(
            produtoRepo.Object, Mock.Of<IRepository<Categoria>>(),
            Mock.Of<IRepository<Marca>>(), Mock.Of<ITributacaoProdutoRepository>(), Mock.Of<IConfiguracaoFiscalService>(),
            _auditoria.Object, new OperadorFixo("Sistema"), NullLogger<ProdutoService>.Instance);

        await servico.RegistrarMovimentacaoAsync(new MovimentacaoEstoqueDto
        {
            ProdutoId = 7, Tipo = TipoMovimentacao.Saida, Quantidade = 2m, Motivo = "Quebra", Usuario = "maria"
        });

        var registro = Assert.Single(_registros);
        Assert.Equal("ESTOQUE_MOVIMENTACAO_MANUAL", registro.Acao);
        Assert.Equal("maria", registro.NomeUsuario);
        Assert.Contains("10 → 8", registro.Descricao);
        Assert.Contains("Quebra", registro.Descricao);
    }

    [Fact]
    public async Task ExcluirCliente_RegistraQuemExcluiuEOsDados()
    {
        var clienteRepo = new Mock<IClienteRepository>();
        clienteRepo.Setup(r => r.ObterPorIdAsync(3)).ReturnsAsync(new Cliente { Id = 3, Nome = "João Pintor", Cpf = "111.444.777-35" });
        clienteRepo.Setup(r => r.PossuiVinculosAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        clienteRepo.Setup(r => r.ExisteFisicamenteAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var servico = new ClienteService(clienteRepo.Object, _auditoria.Object, new OperadorFixo("Dono da Loja"), NullLogger<ClienteService>.Instance);
        await servico.RemoverAsync(3);

        var registro = Assert.Single(_registros);
        Assert.Equal("CLIENTE_EXCLUIDO", registro.Acao);
        Assert.Equal("Dono da Loja", registro.NomeUsuario);
        Assert.Contains("111.444.777-35", registro.PayloadJson);
    }
}
