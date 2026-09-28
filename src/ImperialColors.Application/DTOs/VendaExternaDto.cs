namespace ImperialColors.Application.DTOs;

public class VendaExternaDto
{
    public int Id { get; set; }
    public string NumeroVendaExterna { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>Comissão de quem vendeu, em reais — ver
    /// <see cref="Domain.Entities.VendaExterna.Comissao"/>.</summary>
    public decimal Comissao { get; set; }
    public bool ComissaoPaga { get; set; }
    public DateTime? ComissaoPagaEm { get; set; }

    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataVenda { get; set; }
    public int TotalItens => Itens.Count;
    public List<ItemVendaExternaDto> Itens { get; set; } = new();

    public bool TemComissao => Comissao > 0;

    /// <summary>O que fica para a loja — é este valor que conta como faturamento.</summary>
    public decimal TotalLiquido => Total - Comissao;

    public string SituacaoComissao => !TemComissao
        ? "Sem comissão"
        : ComissaoPaga ? "Paga" : "A pagar";
}

public class ItemVendaExternaDto
{
    public int Id { get; set; }
    public int VendaExternaId { get; set; }
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }

    /// <summary>Comissão deste item — ver
    /// <see cref="Domain.Entities.ItemVendaExterna.Comissao"/>.</summary>
    public decimal Comissao { get; set; }

    public bool ItemManual => !ProdutoId.HasValue;
    public string TipoDescricao => ItemManual ? "Manual" : "Estoque";
    public string DescricaoTroca => $"{NomeProduto} — Qtd: {Quantidade} @ R$ {PrecoUnitario:N2}";
}

public class AtualizarVendaExternaDto
{
    public int Id { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public List<AtualizarItemVendaExternaDto> Itens { get; set; } = new();
}

public class AtualizarItemVendaExternaDto
{
    public int Id { get; set; }
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Comissao { get; set; }
}

public class RegistrarVendaExternaDto
{
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public List<RegistrarItemVendaExternaDto> Itens { get; set; } = new();
}

public class RegistrarItemVendaExternaDto
{
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Comissao { get; set; }
}

/// <summary>
/// Uma linha do arquivo importado ainda em texto cru, antes de qualquer validação — é o
/// formato comum em que TXT, CSV e planilha chegam ao <c>VendaExternaImportHelper</c>. A
/// quantidade é string de propósito: quem decide se "2", "1,5" ou "Quantidade" (cabeçalho)
/// é um número válido é o helper, com a mensagem de erro apontando a linha do arquivo.
/// </summary>
public class LinhaBrutaImportacaoDto
{
    public int NumeroLinha { get; set; }
    public string? CodigoBarras { get; set; }
    public string? NomeProduto { get; set; }
    public string? Quantidade { get; set; }
}

public class LinhaImportacaoVendaExternaDto
{
    public int NumeroLinha { get; set; }
    public string? CodigoBarras { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public int? ProdutoId { get; set; }
    public decimal PrecoBase { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool VinculadoEstoque => ProdutoId.HasValue;
    public string TipoDescricao => VinculadoEstoque ? "Estoque" : "Manual";
}

/// <summary>Filtro da tela de controle de comissões.</summary>
public enum FiltroComissaoVendaExterna
{
    /// <summary>Comissão ainda não repassada — o que a loja deve.</summary>
    APagar = 0,
    Pagas = 1,
    Todas = 2
}

/// <summary>
/// Uma linha do controle de comissões. Só existe para venda externa COM comissão: venda sem
/// comissão não tem nada a pagar e não aparece na tela.
/// </summary>
public class ComissaoVendaExternaDto
{
    public int VendaExternaId { get; set; }
    public string NumeroVendaExterna { get; set; } = string.Empty;
    public DateTime DataVenda { get; set; }
    public string? Usuario { get; set; }
    public decimal TotalVenda { get; set; }
    public decimal Comissao { get; set; }
    public bool Paga { get; set; }
    public DateTime? PagaEm { get; set; }

    public decimal TotalLiquido => TotalVenda - Comissao;
    public string SituacaoDescricao => Paga ? "Paga" : "A pagar";

    /// <summary>Quanto a comissão representa da venda — ajuda a notar um valor digitado
    /// errado (uma comissão de 90% de uma venda dificilmente foi intencional).</summary>
    public decimal PercentualSobreVenda => TotalVenda > 0
        ? Math.Round(Comissao / TotalVenda * 100m, 1)
        : 0m;
}

/// <summary>Totais do painel de comissões — do Dashboard e da tela de controle.</summary>
public class ResumoComissoesDto
{
    public decimal TotalAPagar { get; set; }
    public decimal TotalPago { get; set; }
    public int QuantidadeAPagar { get; set; }
    public int QuantidadePaga { get; set; }

    /// <summary>Comissões do mês corrente, pagas ou não — o custo de vender na rua no mês.</summary>
    public decimal TotalDoMes { get; set; }

    public decimal TotalGeral => TotalAPagar + TotalPago;
    public List<ComissaoVendaExternaDto> Pendentes { get; set; } = new();
}
