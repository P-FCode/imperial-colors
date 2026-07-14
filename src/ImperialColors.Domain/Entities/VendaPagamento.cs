using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class VendaPagamento : BaseEntity
{
    public int VendaId { get; set; }
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Dinheiro;
    public decimal Valor { get; set; }
    public decimal? ValorRecebido { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public int Ordem { get; set; }

    public Venda Venda { get; set; } = null!;
}
