using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class DashboardService : IDashboardService
{
    private const decimal LimiteEstoqueCritico = 5m;

    private readonly IVendaRepository _vendaRepository;
    private readonly IProdutoRepository _produtoRepository;

    public DashboardService(
        IVendaRepository vendaRepository,
        IProdutoRepository produtoRepository)
    {
        _vendaRepository = vendaRepository;
        _produtoRepository = produtoRepository;
    }

    public async Task<DashboardDto> ObterDadosDashboardAsync()
    {
        var hoje = DateTime.Today;

        var totalVendasHoje = await _vendaRepository.ObterTotalVendasDiaAsync(hoje);
        var totalVendasMes = await _vendaRepository.ObterTotalVendasMesAsync(hoje.Year, hoje.Month);
        var totalProdutos = await _produtoRepository.ContarAsync();
        var produtosEstoqueCritico = await _produtoRepository.ContarComEstoqueCriticoAsync(LimiteEstoqueCritico);
        var produtosSemEstoque = (await _produtoRepository.ObterSemEstoqueAsync()).Count();

        var vendasHoje = await _vendaRepository.ObterPorPeriodoAsync(hoje, hoje.AddDays(1).AddSeconds(-1));
        var ultimasVendas = await _vendaRepository.ObterUltimasFinalizadasAsync(5);

        return new DashboardDto
        {
            TotalVendasHoje = totalVendasHoje,
            TotalVendasMes = totalVendasMes,
            QuantidadeVendasHoje = vendasHoje.Count(),
            ProdutosEstoqueCritico = produtosEstoqueCritico,
            ProdutosSemEstoque = produtosSemEstoque,
            TotalProdutos = totalProdutos,
            UltimasVendas = ultimasVendas.Select(v => new VendaResumoDashboardDto
            {
                DataVenda = v.DataVenda,
                FormaPagamentoDescricao = PagamentoHelper.ObterDescricao(v.FormaPagamento, v.QuantidadeParcelas),
                Total = v.Total
            }).ToList()
        };
    }
}
