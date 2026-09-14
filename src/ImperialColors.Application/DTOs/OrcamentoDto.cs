using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;

namespace ImperialColors.Application.DTOs;

public class OrcamentoDto
{
    public int Id { get; set; }
    public string NumeroOrcamento { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string? TelefoneCliente { get; set; }
    public DateTime DataOrcamento { get; set; }
    public DateTime DataValidade { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public StatusOrcamento Status { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public List<ItemOrcamentoDto> Itens { get; set; } = new();

    public int TotalItens => Itens.Count;

    /// <summary>Vencido é calculado na hora, não gravado — ver <see cref="StatusOrcamento"/>.</summary>
    public bool Expirado => Status == StatusOrcamento.Aberto && DataValidade.Date < Relogio.Agora.Date;

    public string StatusDescricao => Status switch
    {
        StatusOrcamento.Aprovado => "Aprovado",
        StatusOrcamento.Recusado => "Recusado",
        _ => Expirado ? "Expirado" : "Aberto"
    };
}

public class ItemOrcamentoDto
{
    public int Id { get; set; }
    public int OrcamentoId { get; set; }
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoProduto { get; set; }
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    public bool ItemManual => !ProdutoId.HasValue;
    public string TipoDescricao => ItemManual ? "Manual" : "Estoque";
}

public class RegistrarOrcamentoDto
{
    public int? ClienteId { get; set; }
    public string NomeCliente { get; set; } = string.Empty;
    public string? TelefoneCliente { get; set; }
    public DateTime DataValidade { get; set; }
    public decimal Desconto { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public List<ItemOrcamentoEntradaDto> Itens { get; set; } = new();
}

public class AtualizarOrcamentoDto : RegistrarOrcamentoDto
{
    public int Id { get; set; }
}

public class ItemOrcamentoEntradaDto
{
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoProduto { get; set; }
    public string? Unidade { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
