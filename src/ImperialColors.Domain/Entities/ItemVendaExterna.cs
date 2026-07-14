namespace ImperialColors.Domain.Entities;

public class ItemVendaExterna : BaseEntity
{
    public int VendaExternaId { get; set; }
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public VendaExterna VendaExterna { get; set; } = null!;
    public Produto? Produto { get; set; }

    public void CalcularSubtotal()
        => Subtotal = Quantidade * PrecoUnitario;
}
