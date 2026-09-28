using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Comissão da venda externa. A regra que amarra tudo é a do faturamento: a comissão é
/// dinheiro que passou pela venda e não fica na loja, então o que conta como faturamento é o
/// líquido. Uma venda de R$ 160 com R$ 30 de comissão vale R$ 130.
/// </summary>
public class ComissaoVendaExternaTests
{
    private static (VendaExternaService Servico, Mock<IVendaExternaRepository> Repositorio) CriarServico()
    {
        var repositorio = new Mock<IVendaExternaRepository>();
        repositorio.Setup(r => r.GerarNumeroVendaExternaAsync(It.IsAny<CancellationToken>())).ReturnsAsync("VE-0001");
        repositorio
            .Setup(r => r.RegistrarTransacionalAsync(
                It.IsAny<VendaExterna>(), It.IsAny<IReadOnlyList<ItemVendaExterna>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((VendaExterna v, IReadOnlyList<ItemVendaExterna> itens, string? _, CancellationToken _) =>
            {
                v.Itens = itens.ToList();
                v.CalcularTotais();
                return Task.FromResult(v);
            });

        var servico = new VendaExternaService(
            repositorio.Object,
            Mock.Of<IProdutoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<IUsuarioAtual>());

        return (servico, repositorio);
    }

    /// <summary>Venda de um item só — a comissão agora é do produto, não da venda.</summary>
    private static RegistrarVendaExternaDto VendaDe(decimal valor, decimal comissao) => new()
    {
        Itens = [new RegistrarItemVendaExternaDto
        {
            NomeProduto = "Tinta", Quantidade = 1, PrecoUnitario = valor, Comissao = comissao
        }]
    };

    // ===== O exemplo do pedido =====

    [Fact]
    public async Task VendaDe160ComComissaoDe30_FaturaCentoETrinta()
    {
        var (servico, _) = CriarServico();

        var venda = await servico.RegistrarAsync(VendaDe(160m, 30m));

        Assert.Equal(160m, venda.Total);        // o que o cliente pagou
        Assert.Equal(30m, venda.Comissao);
        Assert.Equal(130m, venda.TotalLiquido); // o que vira faturamento
        Assert.True(venda.TemComissao);
    }

    [Fact]
    public async Task VendaSemComissao_TemLiquidoIgualAoTotal()
    {
        var (servico, _) = CriarServico();

        var venda = await servico.RegistrarAsync(VendaDe(160m, 0m));

        Assert.Equal(160m, venda.TotalLiquido);
        Assert.False(venda.TemComissao);
        // É o que a mantém fora do controle de comissões: não há acerto a fazer.
        Assert.Equal("Sem comissão", venda.SituacaoComissao);
    }

    // ===== Validação =====

    /// <summary>Comissão maior que a venda faria a loja registrar faturamento negativo no
    /// dia — o dinheiro que saiu seria maior que o que entrou naquela venda.</summary>
    [Fact]
    public async Task ComissaoMaiorQueOValorDoItem_Recusa()
    {
        var (servico, _) = CriarServico();

        var erro = await Assert.ThrowsAsync<DomainException>(() => servico.RegistrarAsync(VendaDe(100m, 150m)));

        Assert.Contains("não pode ser maior que o valor do item", erro.Message);
    }

    /// <summary>
    /// A comissão da venda é a soma da dos itens: um item pode render comissão e outro da
    /// mesma venda não. É esse total que o faturamento desconta e que o controle de acertos
    /// mostra como dívida com o vendedor.
    /// </summary>
    [Fact]
    public async Task ComissaoDaVenda_EhASomaDaDosItens()
    {
        var (servico, _) = CriarServico();

        var venda = await servico.RegistrarAsync(new RegistrarVendaExternaDto
        {
            Itens =
            [
                new RegistrarItemVendaExternaDto { NomeProduto = "Tinta", Quantidade = 1, PrecoUnitario = 160m, Comissao = 30m },
                new RegistrarItemVendaExternaDto { NomeProduto = "Pincel", Quantidade = 2, PrecoUnitario = 20m, Comissao = 5m },
                // Item sem comissão na mesma venda — não soma nada.
                new RegistrarItemVendaExternaDto { NomeProduto = "Lixa", Quantidade = 1, PrecoUnitario = 10m }
            ]
        });

        Assert.Equal(210m, venda.Total);        // 160 + 40 + 10
        Assert.Equal(35m, venda.Comissao);      // 30 + 5
        Assert.Equal(175m, venda.TotalLiquido);
        Assert.Equal(30m, venda.Itens.Single(i => i.NomeProduto == "Tinta").Comissao);
        Assert.Equal(0m, venda.Itens.Single(i => i.NomeProduto == "Lixa").Comissao);
    }

    /// <summary>O limite é por item, não pelo total: uma comissão de R$ 150 num item de
    /// R$ 100 tem que ser barrada mesmo que a venda inteira seja maior que isso.</summary>
    [Fact]
    public async Task ComissaoMaiorQueOItemMasMenorQueAVenda_Recusa()
    {
        var (servico, _) = CriarServico();

        var erro = await Assert.ThrowsAsync<DomainException>(() => servico.RegistrarAsync(new RegistrarVendaExternaDto
        {
            Itens =
            [
                new RegistrarItemVendaExternaDto { NomeProduto = "Tinta", Quantidade = 1, PrecoUnitario = 100m, Comissao = 150m },
                new RegistrarItemVendaExternaDto { NomeProduto = "Balde", Quantidade = 1, PrecoUnitario = 500m }
            ]
        }));

        Assert.Contains("'Tinta'", erro.Message);
    }

    /// <summary>Negativo com sinal trocado viraria acréscimo no faturamento em vez de
    /// desconto.</summary>
    [Fact]
    public async Task ComissaoNegativa_Recusa()
    {
        var (servico, _) = CriarServico();

        var erro = await Assert.ThrowsAsync<DomainException>(() => servico.RegistrarAsync(VendaDe(100m, -10m)));

        Assert.Contains("negativa", erro.Message);
    }

    [Fact]
    public async Task ComissaoIgualAoTotal_EhAceita()
    {
        var (servico, _) = CriarServico();

        var venda = await servico.RegistrarAsync(VendaDe(100m, 100m));

        Assert.Equal(0m, venda.TotalLiquido);
    }

    // ===== Controle de acertos =====

    private static VendaExterna Comissionada(int id, string numero, decimal total, decimal comissao, bool paga)
        => new()
        {
            Id = id,
            NumeroVendaExterna = numero,
            DataVenda = DateTime.Today,
            Total = total,
            Comissao = comissao,
            ComissaoPaga = paga
        };

    [Theory]
    [InlineData(FiltroComissaoVendaExterna.APagar, false)]
    [InlineData(FiltroComissaoVendaExterna.Pagas, true)]
    [InlineData(FiltroComissaoVendaExterna.Todas, null)]
    public async Task ListarComissoes_TraduzOFiltroDaTelaParaORepositorio(
        FiltroComissaoVendaExterna filtro, bool? pagaEsperado)
    {
        var (servico, repositorio) = CriarServico();
        repositorio.Setup(r => r.ListarComComissaoPaginadoAsync(
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([Comissionada(1, "VE-0001", 160m, 30m, paga: false)], 1));

        await servico.ObterComissoesPaginadoAsync(filtro, pagina: 1, itensPorPagina: 50);

        repositorio.Verify(r => r.ListarComComissaoPaginadoAsync(
            pagaEsperado, 1, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListarComissoes_TrazOPercentualEOLiquidoDeCadaVenda()
    {
        var (servico, repositorio) = CriarServico();
        repositorio.Setup(r => r.ListarComComissaoPaginadoAsync(
                It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([Comissionada(1, "VE-0007", 160m, 30m, paga: false)], 1));

        var pagina = await servico.ObterComissoesPaginadoAsync(FiltroComissaoVendaExterna.APagar, 1, 50);
        var comissao = Assert.Single(pagina.Itens);

        Assert.Equal("VE-0007", comissao.NumeroVendaExterna);
        Assert.Equal(130m, comissao.TotalLiquido);
        Assert.Equal(18.8m, comissao.PercentualSobreVenda); // 30 / 160
        Assert.Equal("A pagar", comissao.SituacaoDescricao);
    }

    [Fact]
    public async Task MarcarComissaoPaga_RepassaParaORepositorioERegistraAuditoria()
    {
        var repositorio = new Mock<IVendaExternaRepository>();
        repositorio.Setup(r => r.MarcarComissaoAsync(7, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Comissionada(7, "VE-0007", 160m, 30m, paga: true));

        var auditoria = new Mock<IAuditoriaService>();
        var servico = new VendaExternaService(
            repositorio.Object, Mock.Of<IProdutoRepository>(), auditoria.Object, Mock.Of<IUsuarioAtual>());

        await servico.MarcarComissaoAsync(7, paga: true);

        repositorio.Verify(r => r.MarcarComissaoAsync(7, true, It.IsAny<CancellationToken>()), Times.Once);
        // O acerto mexe em dinheiro — precisa deixar rastro de quem marcou.
        auditoria.Verify(a => a.RegistrarAsync(
            It.Is<RegistrarLogAuditoriaDto>(l => l.Acao == "COMISSAO_PAGA"), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// O Relatório Consolidado de Vendas é uma tela de faturamento, então o Total dele
    /// precisa ser o líquido — o mesmo número do Dashboard. Divergir entre os dois foi o
    /// problema que a própria inclusão da venda externa no Dashboard veio resolver.
    /// </summary>
    [Fact]
    public async Task RelatorioConsolidado_UsaOLiquidoNoTotalEMostraAComissaoEmColunaPropria()
    {
        var vendaExternaService = new Mock<IVendaExternaService>();
        vendaExternaService
            .Setup(s => s.ObterPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VendaExternaDto>
            {
                new()
                {
                    NumeroVendaExterna = "VE-0007",
                    DataVenda = DateTime.Today,
                    Subtotal = 160m,
                    Total = 160m,
                    Comissao = 30m
                }
            });

        var vendaService = new Mock<IVendaService>();
        vendaService.Setup(s => s.ObterPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<VendaDto>());

        var analytics = new RelatorioAnalyticsService(
            Mock.Of<IRelatorioAnalyticsRepository>(), vendaService.Object, vendaExternaService.Object);

        var linha = Assert.Single(
            await analytics.ObterVendasConsolidadasAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)));

        Assert.Equal("Externa", linha.Origem);
        Assert.Equal(160m, linha.Subtotal);  // o que o cliente pagou
        Assert.Equal(30m, linha.Comissao);
        Assert.Equal(130m, linha.Total);     // o que ficou para a loja
        // Comissão não é desconto ao cliente — misturar as duas faria o relatório dizer que
        // ele pagou menos do que pagou.
        Assert.Equal(0m, linha.Desconto);
    }

    [Fact]
    public async Task DesmarcarComissao_RegistraComoEstornoNaAuditoria()
    {
        var repositorio = new Mock<IVendaExternaRepository>();
        repositorio.Setup(r => r.MarcarComissaoAsync(7, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Comissionada(7, "VE-0007", 160m, 30m, paga: false));

        var auditoria = new Mock<IAuditoriaService>();
        var servico = new VendaExternaService(
            repositorio.Object, Mock.Of<IProdutoRepository>(), auditoria.Object, Mock.Of<IUsuarioAtual>());

        await servico.MarcarComissaoAsync(7, paga: false);

        auditoria.Verify(a => a.RegistrarAsync(
            It.Is<RegistrarLogAuditoriaDto>(l => l.Acao == "COMISSAO_ESTORNADA"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
