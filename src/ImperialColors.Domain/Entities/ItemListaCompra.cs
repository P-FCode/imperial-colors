namespace ImperialColors.Domain.Entities;

public class ItemListaCompra : BaseEntity
{
    public int ListaCompraId { get; set; }
    public int? ProdutoId { get; set; }
    public string? DescricaoItem { get; set; }
    public decimal QuantidadeDesejada { get; set; }
    public decimal? QuantidadeComprada { get; set; }
    public bool Comprado { get; set; }
    public string? Observacoes { get; set; }

    public ListaCompra ListaCompra { get; set; } = null!;
    public Produto? Produto { get; set; }
}
