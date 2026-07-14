namespace ImperialColors.Domain.Entities;

public class VendaExterna : BaseEntity
{
    public string NumeroVendaExterna { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.Now;

    public ICollection<ItemVendaExterna> Itens { get; set; } = new List<ItemVendaExterna>();
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = new List<MovimentacaoEstoque>();

    public void CalcularTotais()
    {
        Subtotal = Itens.Sum(i => i.Subtotal);
        Total = Subtotal;
    }
}
