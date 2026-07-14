namespace ImperialColors.Application.DTOs;

public class ProdutoMaisVendidoDto
{
    public string NomeProduto { get; set; } = string.Empty;
    public decimal QuantidadeTotal { get; set; }
    public decimal TotalVendido { get; set; }
}
