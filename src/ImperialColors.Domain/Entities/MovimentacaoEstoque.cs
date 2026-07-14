using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class MovimentacaoEstoque : BaseEntity
{
    public int ProdutoId { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public decimal QuantidadeAnterior { get; set; }
    public decimal QuantidadeAtual { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string? Usuario { get; set; }
    public int? VendaId { get; set; }
    public int? VendaExternaId { get; set; }

    public Produto Produto { get; set; } = null!;
    public Venda? Venda { get; set; }
    public VendaExterna? VendaExterna { get; set; }
}
