namespace ImperialColors.Domain.Entities;

public class ItemVenda : BaseEntity
{
    public int VendaId { get; set; }
    public int ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal Subtotal { get; set; }

    public Venda Venda { get; set; } = null!;
    public Produto Produto { get; set; } = null!;

    public void CalcularSubtotal()
    {
        Subtotal = (Quantidade * PrecoUnitario) - Desconto;
    }
}
