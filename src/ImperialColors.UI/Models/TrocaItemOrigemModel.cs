namespace ImperialColors.UI.Models;

public class TrocaItemOrigemModel
{
    public int Id { get; init; }
    public string NomeExibicao { get; init; } = string.Empty;
    public decimal Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }

    /// <summary>Valor creditado por unidade: preço já descontado (item e rateio da venda).</summary>
    public decimal ValorUnitarioDevolucao { get; init; }

    public int? ProdutoId { get; init; }
    public decimal Subtotal => Quantidade * PrecoUnitario;

    public override string ToString() => NomeExibicao;
}
