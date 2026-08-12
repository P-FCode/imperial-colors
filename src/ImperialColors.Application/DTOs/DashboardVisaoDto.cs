namespace ImperialColors.Application.DTOs;

/// <summary>Dados da visão "Estoque" do Dashboard — reaproveita <see cref="ProdutoDto"/> e
/// <see cref="ProdutoRankingDto"/> já existentes, sem duplicar mapeamento.</summary>
public class DashboardEstoqueDto
{
    public List<ProdutoDto> ProximosDaValidade { get; set; } = new();
    public List<ProdutoDto> PoucaQuantidade { get; set; } = new();
    public List<ProdutoRankingDto> MaisVendidos { get; set; } = new();
}

/// <summary>Dados da visão "Vendas" do Dashboard.</summary>
public class DashboardVendasDto
{
    public List<VendaDestaqueDto> MaioresVendas { get; set; } = new();
}

public class VendaDestaqueDto
{
    public DateTime Data { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string FormaPagamentoDescricao { get; set; } = string.Empty;
}
