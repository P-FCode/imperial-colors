namespace ImperialColors.Domain.Entities;

public class ItemOrcamento : BaseEntity
{
    public int OrcamentoId { get; set; }

    /// <summary>Null quando o item foi digitado à mão (produto que a loja ainda não cadastrou).</summary>
    public int? ProdutoId { get; set; }

    // Nome, código e unidade são congelados no momento do orçamento: o preço proposto precisa
    // continuar fazendo sentido mesmo que o produto seja renomeado ou reajustado depois.
    public string NomeProduto { get; set; } = string.Empty;

    public string? CodigoProduto { get; set; }

    public string? Unidade { get; set; }

    public decimal Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Subtotal { get; set; }

    public Orcamento Orcamento { get; set; } = null!;

    public Produto? Produto { get; set; }

    public void CalcularSubtotal()
        => Subtotal = Quantidade * PrecoUnitario;
}
