using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

public class TrocaDto
{
    public int Id { get; set; }
    public int? VendaOrigemId { get; set; }
    public int? VendaExternaOrigemId { get; set; }
    public string NumeroVendaOrigem { get; set; } = string.Empty;
    public int ProdutoDevolvidoId { get; set; }
    public string ProdutoDevolvidoNome { get; set; } = string.Empty;
    public decimal QuantidadeDevolvida { get; set; }
    public decimal ValorUnitarioDevolucao { get; set; }
    public bool RetornarAoEstoque { get; set; }
    public int ProdutoNovoId { get; set; }
    public string ProdutoNovoNome { get; set; } = string.Empty;
    public decimal QuantidadeNova { get; set; }
    public decimal ValorUnitarioNovo { get; set; }
    public FormaPagamento? FormaPagamentoDiferenca { get; set; }
    public string? FormaPagamentoDiferencaDescricao { get; set; }
    public decimal ValorTotalDevolvido { get; set; }
    public decimal ValorTotalNovo { get; set; }
    public decimal DiferencaValor { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataTroca { get; set; }
}

public class RegistrarTrocaDto
{
    public int VendaOrigemId { get; set; }
    public int ItemVendaOrigemId { get; set; }
    public decimal QuantidadeDevolvida { get; set; }
    public bool RetornarAoEstoque { get; set; }
    public int ProdutoNovoId { get; set; }
    public decimal QuantidadeNova { get; set; }
    public decimal PrecoUnitarioNovo { get; set; }
    public FormaPagamento? FormaPagamentoDiferenca { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
}

public class RegistrarTrocaVendaExternaDto
{
    public int VendaExternaOrigemId { get; set; }
    public int ItemVendaExternaOrigemId { get; set; }
    public decimal QuantidadeDevolvida { get; set; }
    public bool RetornarAoEstoque { get; set; }
    public int ProdutoNovoId { get; set; }
    public decimal QuantidadeNova { get; set; }
    public decimal PrecoUnitarioNovo { get; set; }
    public FormaPagamento? FormaPagamentoDiferenca { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
}
