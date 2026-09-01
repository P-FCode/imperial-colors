using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// <c>ObterResumoAsync</c> é pura remontagem: pega o que o repositório já agregou
/// (<see cref="EstatisticasNotasFiscais"/>) e copia campo a campo para o DTO da tela — a
/// única lógica de verdade que mora aqui é o <c>TicketMedio</c> calculado no próprio DTO.
/// Testado com o repositório mockado porque o que importa é a montagem, não a agregação SQL
/// (essa é responsabilidade de <c>NotaFiscalRepository.ObterEstatisticasAsync</c>, coberta à
/// parte contra o Postgres real).
/// </summary>
public class NotaFiscalResumoServiceTests
{
    private static NotaFiscalService CriarServico(EstatisticasNotasFiscais estatisticas, IReadOnlyList<NotaFiscal>? ultimas = null)
    {
        var repositorio = new Mock<INotaFiscalRepository>();
        repositorio.Setup(r => r.ObterEstatisticasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(estatisticas);
        repositorio.Setup(r => r.ListarUltimasAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ultimas ?? Array.Empty<NotaFiscal>());

        return new NotaFiscalService(
            repositorio.Object,
            Mock.Of<IVendaRepository>(),
            Mock.Of<IClienteRepository>(),
            Mock.Of<IProdutoRepository>(),
            Mock.Of<ITributacaoProdutoRepository>(),
            Mock.Of<ITributacaoCategoriaRepository>(),
            Mock.Of<IRepository<NaturezaOperacao>>(),
            Mock.Of<IConfiguracaoFiscalService>(),
            Mock.Of<IFiscalApiClient>());
    }

    [Fact]
    public async Task ObterResumoAsync_CopiaTodosOsCamposDaAgregacao()
    {
        var estatisticas = new EstatisticasNotasFiscais
        {
            TotalEmitidas = 42,
            ValorTotalEmitido = 18450.75m,
            EmitidasHoje = 3,
            ValorEmitidoHoje = 890m,
            EmitidasNoMes = 15,
            ValorEmitidoNoMes = 6230.40m,
            TotalCanceladas = 9,
            TotalRejeitadas = 4,
            TotalDenegadas = 1,
            TotalPendentes = 2,
            TotalNFe = 30,
            ValorNFe = 15200m,
            TotalNFCe = 12,
            ValorNFCe = 3250.75m
        };

        var resumo = await CriarServico(estatisticas).ObterResumoAsync();

        Assert.Equal(42, resumo.TotalEmitidas);
        Assert.Equal(18450.75m, resumo.ValorTotalEmitido);
        Assert.Equal(3, resumo.EmitidasHoje);
        Assert.Equal(15, resumo.EmitidasNoMes);
        Assert.Equal(9, resumo.TotalCanceladas);
        Assert.Equal(4, resumo.TotalRejeitadas);
        Assert.Equal(1, resumo.TotalDenegadas);
        Assert.Equal(2, resumo.TotalPendentes);
        Assert.Equal(30, resumo.TotalNFe);
        Assert.Equal(12, resumo.TotalNFCe);
        // 18450,75 / 42 — o ticket médio é calculado no próprio DTO, não vem do repositório.
        Assert.Equal(18450.75m / 42, resumo.TicketMedio);
    }

    /// <summary>0 dividido por 0 é indefinido, não zero — a loja que nunca emitiu nota não
    /// pode ver "Ticket médio: R$ 0,00" (parece um erro de cálculo) onde o correto é "sem
    /// dado ainda".</summary>
    [Fact]
    public async Task SemNenhumaNotaEmitida_TicketMedioENuloNaoZero()
    {
        var resumo = await CriarServico(new EstatisticasNotasFiscais { TotalEmitidas = 0, ValorTotalEmitido = 0m }).ObterResumoAsync();

        Assert.Null(resumo.TicketMedio);
    }
}
