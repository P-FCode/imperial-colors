namespace ImperialColors.UI.Models;

public class TrocaItemOrigemModel
{
    public int Id { get; init; }
    public string NomeExibicao { get; init; } = string.Empty;
    public decimal Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }
    public int? ProdutoId { get; init; }
    public decimal Subtotal => Quantidade * PrecoUnitario;

    public override string ToString() => NomeExibicao;
}
