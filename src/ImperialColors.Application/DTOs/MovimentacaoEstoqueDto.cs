using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

public class MovimentacaoEstoqueDto
{
    public int ProdutoId { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Usuario { get; set; }
}
