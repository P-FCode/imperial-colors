namespace ImperialColors.Application.DTOs;

public class VendaExternaDto
{
    public int Id { get; set; }
    public string NumeroVendaExterna { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataVenda { get; set; }
    public int TotalItens => Itens.Count;
    public List<ItemVendaExternaDto> Itens { get; set; } = new();
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
