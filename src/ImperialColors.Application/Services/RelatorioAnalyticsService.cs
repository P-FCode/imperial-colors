using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class RelatorioAnalyticsService : IRelatorioAnalyticsService
{
    private readonly IRelatorioAnalyticsRepository _repository;
    private readonly IVendaService _vendaService;
    private readonly IVendaExternaService _vendaExternaService;

    public RelatorioAnalyticsService(
        IRelatorioAnalyticsRepository repository,
        IVendaService vendaService,
        IVendaExternaService vendaExternaService)
    {
        _repository = repository;
        _vendaService = vendaService;
        _vendaExternaService = vendaExternaService;
    }

    public async Task<IReadOnlyList<LinhaRelatorioVendaExternaDto>> ObterLinhasVendasExternasAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        var linhas = await _repository.ObterLinhasVendasExternasPorPeriodoAsync(inicio, fim, cancellationToken);
        return linhas.Select(l => new LinhaRelatorioVendaExternaDto
        {
            DataVenda = l.DataVenda,
            CodigoVenda = l.CodigoVenda,
            ProdutoItem = l.ProdutoItem,
            QuantidadeVendida = l.QuantidadeVendida,
            ValorUnitario = l.ValorUnitario,
            ValorTotal = l.ValorTotal
        }).ToList();
    }

    public async Task<IReadOnlyList<ProdutoRankingDto>> ObterRankingProdutosAsync(
        DateTime inicio, DateTime fim, TipoAnaliseGiroProduto tipo, CancellationToken cancellationToken = default)
    {
        var ranking = tipo switch
        {
            TipoAnaliseGiroProduto.MaisVendidos =>
                await _repository.ObterProdutosMaisVendidosAsync(inicio, fim, cancellationToken),
            TipoAnaliseGiroProduto.MenosVendidos =>
                await _repository.ObterProdutosMenosVendidosAsync(inicio, fim, cancellationToken),
            _ => Array.Empty<Domain.ReadModels.ProdutoRankingResumo>()
        };

        return ranking
            .Select((r, index) => new ProdutoRankingDto
            {
                Posicao = index + 1,
                CodigoInterno = r.CodigoInterno,
                NomeProduto = r.NomeProduto,
                QuantidadeTotal = r.QuantidadeTotal,
                FaturamentoGerado = r.FaturamentoGerado
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ProdutoEncalhadoDto>> ObterProdutosEncalhadosAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        var itens = await _repository.ObterProdutosNuncaVendidosAsync(inicio, fim, cancellationToken);
        return itens.Select(i => new ProdutoEncalhadoDto
        {
            CodigoInterno = i.CodigoInterno,
            NomeProduto = i.NomeProduto,
            EstoqueAtual = i.EstoqueAtual,
            ValorTotalParado = i.ValorTotalParado
        }).ToList();
    }

    public async Task<IReadOnlyList<LinhaRelatorioVendaConsolidadaDto>> ObterVendasConsolidadasAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        var vendasBalcao = await _vendaService.ObterPorPeriodoAsync(inicio, fim);
        var vendasExternas = await _vendaExternaService.ObterTodosAsync(cancellationToken);

        var linhas = new List<LinhaRelatorioVendaConsolidadaDto>();

        foreach (var venda in vendasBalcao)
        {
            linhas.Add(new LinhaRelatorioVendaConsolidadaDto
            {
                DataVenda = venda.DataVenda,
                Origem = "Balcão",
                NumeroVenda = venda.NumeroVenda,
                ClienteOuResumo = venda.ClienteNome ?? venda.NomeCompradorExibicao,
                TotalItens = venda.Itens?.Count ?? 0,
                Subtotal = venda.Subtotal,
                Desconto = venda.Desconto,
                Total = venda.Total,
                FormaPagamento = venda.FormaPagamentoDescricao
            });
        }

        foreach (var venda in vendasExternas.Where(v => v.DataVenda >= inicio && v.DataVenda <= fim))
        {
            linhas.Add(new LinhaRelatorioVendaConsolidadaDto
            {
                DataVenda = venda.DataVenda,
                Origem = "Externa",
                NumeroVenda = venda.NumeroVendaExterna,
                ClienteOuResumo = string.IsNullOrWhiteSpace(venda.Observacoes) ? "Venda de rua" : venda.Observacoes!,
                TotalItens = venda.TotalItens,
                Subtotal = venda.Subtotal,
                Desconto = 0,
                Total = venda.Total,
                FormaPagamento = "Venda Externa"
            });
        }

        return linhas
            .OrderByDescending(l => l.DataVenda)
            .ThenBy(l => l.NumeroVenda)
            .ToList();
    }
}
