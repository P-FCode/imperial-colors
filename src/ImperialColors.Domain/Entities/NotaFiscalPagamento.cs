using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Linha de pagamento de uma <see cref="NotaFiscal"/> — espelha <c>pag.detPag[]</c> da
/// NF-e/NFC-e. Mesmo shape de <see cref="VendaPagamento"/> de propósito (mesmo
/// <see cref="Helpers.FormaPagamentoFiscalMapper"/> serve para os dois), mas como
/// registro próprio: uma nota avulsa (fora do fluxo de venda) não tem uma Venda para
/// buscar os pagamentos.
/// </summary>
public class NotaFiscalPagamento : BaseEntity
{
    public int NotaFiscalId { get; set; }
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Dinheiro;
    public decimal Valor { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public int Ordem { get; set; }

    public NotaFiscal NotaFiscal { get; set; } = null!;
}
