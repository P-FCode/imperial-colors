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

    /// <summary>
    /// Comissão deste item, em reais. Fica no item e não na venda porque é assim que o
    /// acerto é combinado na rua: cada produto tem a sua — um item pode render comissão e
    /// outro da mesma venda não. A comissão da venda
    /// (<see cref="VendaExterna.Comissao"/>) é a soma destas.
    /// </summary>
    public decimal Comissao { get; set; }

    public VendaExterna VendaExterna { get; set; } = null!;
    public Produto? Produto { get; set; }

    public void CalcularSubtotal()
        => Subtotal = Quantidade * PrecoUnitario;
}
