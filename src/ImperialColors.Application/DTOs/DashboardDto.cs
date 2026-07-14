namespace ImperialColors.Application.DTOs;

public class DashboardDto
{
    public decimal TotalVendasHoje { get; set; }
    public decimal TotalVendasMes { get; set; }
    public int QuantidadeVendasHoje { get; set; }
    public int ProdutosEstoqueCritico { get; set; }
    public int ProdutosSemEstoque { get; set; }
    public int TotalProdutos { get; set; }
    public List<VendaResumoDashboardDto> UltimasVendas { get; set; } = new();
}

public class VendaResumoDashboardDto
{
    public DateTime DataVenda { get; set; }
    public string HoraVenda => DataVenda.ToString("HH:mm");
    public string FormaPagamentoDescricao { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ProdutoBaixoEstoqueDto
{
    public string Nome { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public string Unidade { get; set; } = "UN";
}
