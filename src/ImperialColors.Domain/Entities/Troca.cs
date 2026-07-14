using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class Troca : BaseEntity
{
    public int? VendaOrigemId { get; set; }
    public int? VendaExternaOrigemId { get; set; }

    public int ProdutoDevolvidoId { get; set; }
    public decimal QuantidadeDevolvida { get; set; }
    public decimal ValorUnitarioDevolucao { get; set; }
    public bool RetornarAoEstoque { get; set; }

    public int ProdutoNovoId { get; set; }
    public decimal QuantidadeNova { get; set; }
    public decimal ValorUnitarioNovo { get; set; }

    public FormaPagamento? FormaPagamentoDiferenca { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataTroca { get; set; } = DateTime.UtcNow;

    public Venda? VendaOrigem { get; set; }
    public VendaExterna? VendaExternaOrigem { get; set; }
    public Produto ProdutoDevolvido { get; set; } = null!;
    public Produto ProdutoNovo { get; set; } = null!;

    public decimal ValorTotalDevolvido => QuantidadeDevolvida * ValorUnitarioDevolucao;
    public decimal ValorTotalNovo => QuantidadeNova * ValorUnitarioNovo;
    public decimal DiferencaValor => ValorTotalNovo - ValorTotalDevolvido;
}
