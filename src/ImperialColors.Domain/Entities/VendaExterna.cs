namespace ImperialColors.Domain.Entities;

public class VendaExterna : BaseEntity
{
    public string NumeroVendaExterna { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.Now;

    /// <summary>
    /// Quanto da venda vai para quem vendeu na rua, em reais. Zero quando a venda não gera
    /// comissão — e é justamente esse zero que decide se ela aparece no controle de
    /// comissões: venda sem comissão não tem nada a pagar e não vira pendência.
    /// </summary>
    public decimal Comissao { get; set; }

    /// <summary>Comissão já repassada. Só faz sentido com <see cref="Comissao"/> maior que
    /// zero; o pagamento é marcado na tela de comissões.</summary>
    public bool ComissaoPaga { get; set; }

    /// <summary>Quando a comissão foi marcada como paga — guardado para o histórico do
    /// acerto, já que "pago" sozinho não diz de quando.</summary>
    public DateTime? ComissaoPagaEm { get; set; }

    public ICollection<ItemVendaExterna> Itens { get; set; } = new List<ItemVendaExterna>();
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = new List<MovimentacaoEstoque>();

    public bool TemComissao => Comissao > 0;

    /// <summary>
    /// O que de fato entrou para a loja: o total cobrado menos a comissão. É este valor que
    /// conta como faturamento — a comissão é dinheiro que passou pela venda mas não fica.
    ///
    /// <see cref="Total"/> continua sendo o valor cheio, o que o cliente pagou: é ele que
    /// aparece na venda, no comprovante e na conferência com o vendedor.
    /// </summary>
    public decimal TotalLiquido => Total - Comissao;

    public void CalcularTotais()
    {
        Subtotal = Itens.Sum(i => i.Subtotal);
        Total = Subtotal;
    }
}
