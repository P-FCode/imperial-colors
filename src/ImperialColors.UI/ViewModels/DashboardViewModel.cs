using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;

namespace ImperialColors.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IDashboardService _dashboardService;

    private decimal _totalVendasHoje;
    public decimal TotalVendasHoje { get => _totalVendasHoje; set => SetProperty(ref _totalVendasHoje, value); }

    private decimal _totalVendasMes;
    public decimal TotalVendasMes { get => _totalVendasMes; set => SetProperty(ref _totalVendasMes, value); }

    private int _produtosEstoqueCritico;
    public int ProdutosEstoqueCritico { get => _produtosEstoqueCritico; set => SetProperty(ref _produtosEstoqueCritico, value); }

    private int _produtosSemEstoque;
    public int ProdutosSemEstoque { get => _produtosSemEstoque; set => SetProperty(ref _produtosSemEstoque, value); }

    private List<VendaResumoDashboardDto> _ultimasVendas = new();
    public List<VendaResumoDashboardDto> UltimasVendas { get => _ultimasVendas; set => SetProperty(ref _ultimasVendas, value); }

    public string DataHoje => DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));

    public AsyncRelayCommand CarregarCommand { get; }

    public DashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        CarregarCommand = new AsyncRelayCommand(CarregarDados);
    }

    public async Task CarregarDados()
    {
        try
        {
            Carregando = true;
            var dados = await _dashboardService.ObterDadosDashboardAsync();
            TotalVendasHoje = dados.TotalVendasHoje;
            TotalVendasMes = dados.TotalVendasMes;
            ProdutosEstoqueCritico = dados.ProdutosEstoqueCritico;
            ProdutosSemEstoque = dados.ProdutosSemEstoque;
            UltimasVendas = dados.UltimasVendas;
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar dashboard: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }
}
