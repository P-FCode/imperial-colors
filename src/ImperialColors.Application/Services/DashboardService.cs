using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class DashboardService : IDashboardService
{
    private const decimal LimiteEstoqueCritico = 5m;
    private const int DiasLimiteValidadeProxima = 15;
    private const int TopEstoque = 10;
    private const int TopDestaques = 5;

    private readonly IVendaRepository _vendaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IProdutoService _produtoService;
    private readonly IRelatorioAnalyticsService _relatorioAnalyticsService;

    public DashboardService(
        IVendaRepository vendaRepository,
        IProdutoRepository produtoRepository,
        IProdutoService produtoService,
        IRelatorioAnalyticsService relatorioAnalyticsService)
    {
        _vendaRepository = vendaRepository;
        _produtoRepository = produtoRepository;
        _produtoService = produtoService;
        _relatorioAnalyticsService = relatorioAnalyticsService;
    }

    public async Task<DashboardDto> ObterDadosDashboardAsync()
    {
        var hoje = DateTime.Today;
        var (inicioMes, fimMes) = ObterMesAtual(hoje);
        var inicioUltimos7Dias = hoje.AddDays(-6);

        var totalVendasHoje = await _vendaRepository.ObterTotalVendasDiaAsync(hoje);
        var totalVendasMes = await _vendaRepository.ObterTotalVendasMesAsync(hoje.Year, hoje.Month);
        var totalProdutos = await _produtoRepository.ContarAsync();
        var produtosEstoqueCritico = await _produtoRepository.ContarComEstoqueCriticoAsync(LimiteEstoqueCritico);
        var produtosSemEstoque = (await _produtoRepository.ObterSemEstoqueAsync()).Count();

        // ObterPorPeriodoAsync já filtra Status == Finalizada e inclui Itens.Produto — é o
        // que permite calcular custo (Produto.Custo) por item vendido, não só o faturamento.
        var vendasHoje = (await _vendaRepository.ObterPorPeriodoAsync(hoje, hoje.AddDays(1).AddSeconds(-1))).ToList();
        var vendasMes = (await _vendaRepository.ObterPorPeriodoAsync(inicioMes, fimMes)).ToList();

        // Consulta separada (não reaproveita vendasMes) porque os últimos 7 dias podem
        // cruzar a virada do mês (ex.: dia 3 do mês olhando os últimos 7 dias inclui
        // dias do mês anterior) — matematicamente mais simples e seguro que tentar
        // recombinar dois períodos na mão.
        var vendasUltimos7Dias = await _vendaRepository.ObterPorPeriodoAsync(inicioUltimos7Dias, hoje.AddDays(1).AddSeconds(-1));

        var (faturamentoHoje, custoHoje, lucroHoje, _) = CalcularLucro(vendasHoje);
        var (faturamentoMes, custoMes, lucroMes, itensSemCustoMes) = CalcularLucro(vendasMes);

        var lucroPorDia = vendasUltimos7Dias
            .GroupBy(v => v.DataVenda.Date)
            .ToDictionary(g => g.Key, g => CalcularLucro(g));

        var lucroUltimos7Dias = new List<LucroDiarioDto>();
        for (var i = 6; i >= 0; i--)
        {
            var dia = hoje.AddDays(-i);
            var (faturamentoDia, custoDia, lucroDia, _) = lucroPorDia.TryGetValue(dia, out var valores)
                ? valores : (0m, 0m, 0m, 0);

            lucroUltimos7Dias.Add(new LucroDiarioDto
            {
                Data = dia,
                Faturamento = faturamentoDia,
                Custo = custoDia,
                Lucro = lucroDia
            });
        }

        var maiorFaturamentoDoPeriodo = lucroUltimos7Dias.Count > 0 ? lucroUltimos7Dias.Max(d => d.Faturamento) : 0m;
        foreach (var dia in lucroUltimos7Dias)
            dia.PercentualBarra = maiorFaturamentoDoPeriodo > 0
                ? Math.Round(dia.Faturamento / maiorFaturamentoDoPeriodo * 100m, 1)
                : 0m;

        return new DashboardDto
        {
            TotalVendasHoje = totalVendasHoje,
            TotalVendasMes = totalVendasMes,
            QuantidadeVendasHoje = vendasHoje.Count,
            ProdutosEstoqueCritico = produtosEstoqueCritico,
            ProdutosSemEstoque = produtosSemEstoque,
            TotalProdutos = totalProdutos,
            LucroHoje = lucroHoje,
            LucroMes = lucroMes,
            CustoHoje = custoHoje,
            CustoMes = custoMes,
            MargemLucroHoje = faturamentoHoje > 0 ? Math.Round(lucroHoje / faturamentoHoje * 100m, 1) : 0m,
            MargemLucroMes = faturamentoMes > 0 ? Math.Round(lucroMes / faturamentoMes * 100m, 1) : 0m,
            TicketMedioMes = vendasMes.Count > 0 ? Math.Round(faturamentoMes / vendasMes.Count, 2) : 0m,
            QuantidadeVendasMes = vendasMes.Count,
            ItensSemCustoCadastradoMes = itensSemCustoMes,
            LucroUltimos7Dias = lucroUltimos7Dias
        };
    }

    public async Task<DashboardEstoqueDto> ObterVisaoEstoqueAsync(CancellationToken cancellationToken = default)
    {
        var (inicioMes, fimMes) = ObterMesAtual(DateTime.Today);

        // Reaproveita os serviços já existentes (Relatórios/Estoque) em vez de duplicar
        // consulta/mapeamento aqui — mesmo padrão que RelatorioAnalyticsService já usa
        // (injetar outro Application service, não só repositórios).
        var proximosValidade = await _produtoService.ObterProximosDaValidadeAsync(DiasLimiteValidadeProxima);
        var poucaQuantidade = await _produtoService.ObterComEstoqueBaixoAsync();
        var maisVendidos = await _relatorioAnalyticsService.ObterRankingProdutosAsync(
            inicioMes, fimMes, TipoAnaliseGiroProduto.MaisVendidos, cancellationToken);

        return new DashboardEstoqueDto
        {
            ProximosDaValidade = proximosValidade.OrderBy(p => p.DataValidade).Take(TopEstoque).ToList(),
            PoucaQuantidade = poucaQuantidade.OrderBy(p => p.QuantidadeEstoque).Take(TopEstoque).ToList(),
            MaisVendidos = maisVendidos.Take(TopDestaques).ToList()
        };
    }

    public async Task<DashboardVendasDto> ObterVisaoVendasAsync(CancellationToken cancellationToken = default)
    {
        var (inicioMes, fimMes) = ObterMesAtual(DateTime.Today);
        var vendasMes = await _vendaRepository.ObterPorPeriodoAsync(inicioMes, fimMes);

        var maioresVendas = vendasMes
            .OrderByDescending(v => v.Total)
            .Take(TopDestaques)
            .Select(v => new VendaDestaqueDto
            {
                Data = v.DataVenda,
                ClienteNome = v.Cliente?.Nome ?? v.NomeCompradorCupom ?? "Consumidor",
                Total = v.Total,
                FormaPagamentoDescricao = PagamentoHelper.ObterDescricao(v.FormaPagamento, v.QuantidadeParcelas)
            })
            .ToList();

        return new DashboardVendasDto { MaioresVendas = maioresVendas };
    }

    private static (DateTime Inicio, DateTime Fim) ObterMesAtual(DateTime hoje)
    {
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        return (inicioMes, inicioMes.AddMonths(1).AddSeconds(-1));
    }

    /// <summary>
    /// Faturamento = soma de <c>Venda.Total</c> (já líquido de desconto de item e de
    /// cabeçalho — mesma base usada em <see cref="IVendaRepository.ObterTotalVendasDiaAsync"/>).
    /// Custo = soma de <c>Produto.Custo × Quantidade</c> por item vendido; itens cujo
    /// produto não tem custo cadastrado (<c>Custo</c> nulo) não entram nessa soma e são
    /// contados separadamente em <c>ItensSemCusto</c> — sem histórico de custo por venda no
    /// domínio, o custo "no momento da venda" não existe, então usamos o custo ATUAL do
    /// produto como melhor aproximação disponível (ver <see cref="Produto.Custo"/>).
    /// </summary>
    private static (decimal Faturamento, decimal Custo, decimal Lucro, int ItensSemCusto) CalcularLucro(IEnumerable<Venda> vendas)
    {
        var faturamento = 0m;
        var custo = 0m;
        var itensSemCusto = 0;

        foreach (var venda in vendas)
        {
            faturamento += venda.Total;

            foreach (var item in venda.Itens)
            {
                if (item.Produto?.Custo is { } custoUnitario)
                    custo += custoUnitario * item.Quantidade;
                else
                    itensSemCusto++;
            }
        }

        return (faturamento, custo, faturamento - custo, itensSemCusto);
    }
}
