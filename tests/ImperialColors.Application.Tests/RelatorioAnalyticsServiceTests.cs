using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

public class RelatorioAnalyticsServiceTests
{
    [Fact]
    public async Task ObterRankingProdutos_MaisVendidos_DeveNumerarPosicoes()
    {
        var repo = new Mock<IRelatorioAnalyticsRepository>();
        repo.Setup(r => r.ObterProdutosMaisVendidosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProdutoRankingResumo>
            {
                new() { CodigoInterno = "A001", NomeProduto = "Tinta A", QuantidadeTotal = 10, FaturamentoGerado = 500 },
                new() { CodigoInterno = "B002", NomeProduto = "Rolo B", QuantidadeTotal = 3, FaturamentoGerado = 90 }
            });

        var vendaService = new Mock<IVendaService>();
        var vendaExternaService = new Mock<IVendaExternaService>();
        var service = new RelatorioAnalyticsService(repo.Object, vendaService.Object, vendaExternaService.Object);
        var resultado = await service.ObterRankingProdutosAsync(
            DateTime.Today.AddDays(-7), DateTime.Today, TipoAnaliseGiroProduto.MaisVendidos);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(1, resultado[0].Posicao);
        Assert.Equal("Tinta A", resultado[0].NomeProduto);
        Assert.Equal(2, resultado[1].Posicao);
    }

    [Fact]
    public async Task ObterLinhasVendasExternas_DeveMapearDto()
    {
        var data = new DateTime(2026, 6, 20, 14, 30, 0);
        var repo = new Mock<IRelatorioAnalyticsRepository>();
        repo.Setup(r => r.ObterLinhasVendasExternasPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinhaRelatorioVendaExternaResumo>
            {
                new()
                {
                    DataVenda = data,
                    CodigoVenda = "EXT-20260620-0001",
                    ProdutoItem = "Tinta Coral",
                    QuantidadeVendida = 2,
                    ValorUnitario = 45.50m,
                    ValorTotal = 91m
                }
            });

        var vendaService = new Mock<IVendaService>();
        var vendaExternaService = new Mock<IVendaExternaService>();
        var service = new RelatorioAnalyticsService(repo.Object, vendaService.Object, vendaExternaService.Object);
        var linhas = await service.ObterLinhasVendasExternasAsync(DateTime.Today.AddDays(-1), DateTime.Today);

        Assert.Single(linhas);
        Assert.Equal("EXT-20260620-0001", linhas[0].CodigoVenda);
        Assert.Equal(91m, linhas[0].ValorTotal);
    }
}
