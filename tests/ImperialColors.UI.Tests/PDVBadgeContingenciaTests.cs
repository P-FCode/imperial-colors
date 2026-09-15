using System.Reflection;
using System.Windows.Controls;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Services;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// O badge de status do PDV passou a combinar duas informações: se está gravando direto no
/// servidor e quantas vendas ainda não subiram. O caso que motivou a mudança é o terceiro
/// abaixo — ONLINE com pendências: antes o badge voltava a ficar verde assim que a conexão
/// retornava, sem nenhum indício de que ainda havia vendas gravadas só na máquina.
///
/// Testa através do controle real (a janela é construída de verdade), não de um mock da
/// lógica — é o que garante que a dependência nova de <see cref="IDataSyncService"/> está
/// resolvível e que o badge existe com os nomes esperados.
/// </summary>
public class PDVBadgeContingenciaTests
{
    public PDVBadgeContingenciaTests() => WpfTestBootstrap.Inicializar();

    private static PDVView CriarPdv(bool online, int pendentes)
    {
        var health = new Mock<IDatabaseHealthService>();
        health.SetupGet(h => h.IsOnline).Returns(online);

        var contingencia = new Mock<IContingencyVendaService>();
        contingencia.Setup(c => c.ContarPendentesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(pendentes);

        return new PDVView(
            Mock.Of<IProdutoService>(),
            Mock.Of<IVendaService>(),
            Mock.Of<IClienteService>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ISessaoService>(),
            health.Object,
            contingencia.Object,
            Mock.Of<IDataSyncService>());
    }

    /// <summary>Invoca o método privado que pinta o badge, com o contador já ajustado.</summary>
    private static (string Texto, string? Dica) RenderizarBadge(
        bool online, int pendentes, IReadOnlyList<ImperialColors.Application.DTOs.PendenciaSincronizacaoDto>? comErro = null)
    {
        var pdv = CriarPdv(online, pendentes);

        var campo = typeof(PDVView).GetField("_pendentesSincronizacao",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        campo.SetValue(pdv, pendentes);

        if (comErro is not null)
            typeof(PDVView).GetField("_pendenciasComErro", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(pdv, comErro);

        var metodo = typeof(PDVView).GetMethod("AtualizarBadgeStatusRede",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        metodo.Invoke(pdv, [online]);

        var texto = (TextBlock)pdv.FindName("TxtStatusRede")!;
        var borda = (Border)pdv.FindName("BadgeStatusRede")!;
        var resultado = (texto.Text, borda.ToolTip?.ToString());

        pdv.Close();
        return resultado;
    }

    /// <summary>
    /// M6: venda offline recusada pelo servidor não pode aparecer como "subindo automaticamente".
    /// O operador precisa ver qual venda está travada e o motivo, para corrigir a causa.
    /// </summary>
    [StaFact]
    public void OnlineComVendaRecusada_MostraAVendaEOMotivo()
    {
        var (texto, dica) = RenderizarBadge(online: true, pendentes: 2, comErro:
        [
            new() { NumeroTemporario = "OFF-20260915-ABCD1234", DataVenda = new DateTime(2026, 9, 15, 10, 30, 0), Total = 125m,
                    Erro = "Estoque insuficiente para 'Tinta Acrílica 18L'. Disponível: 1." }
        ]);

        Assert.Contains("1 venda(s) offline com erro", texto);
        Assert.Contains("OFF-20260915-ABCD1234", dica);
        Assert.Contains("Estoque insuficiente", dica);
        Assert.DoesNotContain("automaticamente; este aviso some", dica);
    }

    [StaFact]
    public void OnlineSemPendencias_MostraVerdeSemContador()
    {
        var (texto, dica) = RenderizarBadge(online: true, pendentes: 0);

        Assert.Equal("🟢 Online", texto);
        Assert.DoesNotContain("sincronizar", texto);
        Assert.Contains("Nenhuma venda pendente", dica);
    }

    [StaFact]
    public void Offline_AvisaQueAsVendasContinuamSendoRegistradas()
    {
        var (texto, dica) = RenderizarBadge(online: false, pendentes: 3);

        Assert.Contains("Offline", texto);
        Assert.Contains("3 vendas a sincronizar", texto);
        Assert.Contains("sobem sozinhas", dica);
    }

    /// <summary>
    /// O caso que a mudança existe para resolver: conexão de volta, mas ainda há vendas
    /// gravadas apenas nesta máquina. Não pode aparecer como "tudo certo".
    /// </summary>
    [StaFact]
    public void OnlineComPendencias_NaoSeApresentaComoResolvido()
    {
        var (texto, dica) = RenderizarBadge(online: true, pendentes: 2);

        Assert.Contains("Online", texto);
        Assert.Contains("2 vendas a sincronizar", texto);
        Assert.DoesNotContain("🟢", texto);
        Assert.Contains("subindo para o servidor", dica);
    }

    [StaFact]
    public void UmaPendencia_UsaSingular()
    {
        var (texto, _) = RenderizarBadge(online: true, pendentes: 1);

        Assert.Contains("1 venda a sincronizar", texto);
        Assert.DoesNotContain("vendas", texto);
    }
}
