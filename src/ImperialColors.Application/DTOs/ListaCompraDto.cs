namespace ImperialColors.Application.DTOs;

public class ListaCompraDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public bool Finalizada { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadoEm { get; set; }
    public bool PossuiNotaFiscal { get; set; }
    public string? NotaFiscalNomeArquivo { get; set; }
    public string NotaFiscalDescricao => PossuiNotaFiscal ? "Sim" : "Não";
    public List<ItemListaCompraDto> Itens { get; set; } = new();
    public int TotalItens => Itens.Count;
    public int ItensComprados => Itens.Count(i => i.Comprado);
    public string ProgressoDescricao => TotalItens == 0 ? "Sem itens" : $"{ItensComprados}/{TotalItens} comprados";
    public string StatusDescricao => Finalizada ? "Finalizada" : "Em andamento";
}

public class ItemListaCompraDto
{
    public int Id { get; set; }
    public int ListaCompraId { get; set; }
    public int? ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public bool ItemManual => !ProdutoId.HasValue;
    public decimal QuantidadeDesejada { get; set; }
    public decimal? QuantidadeComprada { get; set; }
    public bool Comprado { get; set; }
    public string? Observacoes { get; set; }
    public string Unidade { get; set; } = "UN";
}

public class SalvarListaCompraDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? FornecedorId { get; set; }
    public string? Observacoes { get; set; }
    public List<SalvarItemListaCompraDto> Itens { get; set; } = new();
}

public class SalvarItemListaCompraDto
{
    public int? ProdutoId { get; set; }
    public string? NomeManual { get; set; }
    public decimal QuantidadeDesejada { get; set; }
    public decimal? QuantidadeComprada { get; set; }
    public bool Comprado { get; set; }
    public string? Observacoes { get; set; }
}
