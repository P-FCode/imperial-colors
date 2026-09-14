using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Proposta de preço entregue ao cliente, sem compromisso: não baixa estoque, não gera
/// movimentação financeira e não vira documento fiscal. Aprovar um orçamento apenas registra
/// a decisão do cliente — a venda continua sendo lançada pelo PDV.
/// </summary>
public class Orcamento : BaseEntity
{
    public string NumeroOrcamento { get; set; } = string.Empty;

    public int? ClienteId { get; set; }

    /// <summary>
    /// Nome impresso no orçamento. É uma cópia, e não só a referência ao cadastro, porque o
    /// PDF precisa continuar coerente se o cliente for renomeado ou excluído depois — e
    /// porque orçamento para quem ainda não é cliente é o caso mais comum no balcão.
    /// </summary>
    public string NomeCliente { get; set; } = string.Empty;

    public string? TelefoneCliente { get; set; }

    public DateTime DataOrcamento { get; set; } = Relogio.Agora;

    public DateTime DataValidade { get; set; } = Relogio.Agora.AddDays(7);

    public decimal Subtotal { get; set; }

    public decimal Desconto { get; set; }

    public decimal Total { get; set; }

    public StatusOrcamento Status { get; set; } = StatusOrcamento.Aberto;

    public string? Observacoes { get; set; }

    public string? Usuario { get; set; }

    public Cliente? Cliente { get; set; }

    public ICollection<ItemOrcamento> Itens { get; set; } = new List<ItemOrcamento>();

    public void CalcularTotais()
    {
        Subtotal = Itens.Sum(i => i.Subtotal);
        Total = Math.Max(0, Subtotal - Desconto);
    }
}
