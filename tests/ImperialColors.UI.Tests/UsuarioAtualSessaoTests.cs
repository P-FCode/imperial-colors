using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// A2: a auditoria usa IUsuarioAtual. AddApplication registra um padrão ("Sistema") com TryAdd;
/// o App registra a sessão depois — este teste reproduz essa ordem e garante que vale a sessão.
/// </summary>
public class UsuarioAtualSessaoTests
{
    [Fact]
    public void RegistroDoApp_EntregaAoServicoOUsuarioLogadoNaSessao()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<SessaoService>();
        services.AddSingleton<ISessaoService>(sp => sp.GetRequiredService<SessaoService>());
        services.AddSingleton<IUsuarioAtual>(sp => sp.GetRequiredService<SessaoService>());

        using var provider = services.BuildServiceProvider();
        var usuarioAtual = provider.GetRequiredService<IUsuarioAtual>();
        var sessao = provider.GetRequiredService<ISessaoService>();

        Assert.Equal("Sistema", usuarioAtual.Nome);

        sessao.IniciarSessao(new UsuarioSessaoDto { Id = Guid.NewGuid(), NomeCompleto = "Maria Caixa", Username = "maria", Permissao = PermissaoUsuario.Caixa });
        Assert.Equal("Maria Caixa", usuarioAtual.Nome);

        sessao.EncerrarSessao();
        Assert.Equal("Sistema", usuarioAtual.Nome);
    }
}
