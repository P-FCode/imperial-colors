namespace ImperialColors.Infrastructure.Contingency;

public class VendaContingencia
{
    public int Id { get; set; }
    public Guid ContingenciaId { get; set; }
    public string NumeroTemporario { get; set; } = string.Empty;
    public DateTime DataVenda { get; set; }
    public int? ClienteId { get; set; }
    public bool ConsumidorFinal { get; set; }
    public string? NomeComprador { get; set; }
    public string? DocumentoComprador { get; set; }
    public int? TipoPessoaComprador { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public bool PendenteSincronizacao { get; set; } = true;
    public DateTime? SincronizadoEm { get; set; }
    public int? VendaServidorId { get; set; }
    public string? ErroSincronizacao { get; set; }

    public ICollection<ItemVendaContingencia> Itens { get; set; } = new List<ItemVendaContingencia>();
    public ICollection<PagamentoContingencia> Pagamentos { get; set; } = new List<PagamentoContingencia>();
}

public class ItemVendaContingencia
{
    public int Id { get; set; }
    public int VendaContingenciaId { get; set; }
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal Subtotal { get; set; }
    public VendaContingencia? Venda { get; set; }
}

public class PagamentoContingencia
{
    public int Id { get; set; }
    public int VendaContingenciaId { get; set; }
    public int FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public decimal? ValorRecebido { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public int Ordem { get; set; }
    public VendaContingencia? Venda { get; set; }
}

public class EstoqueLocalCache
{
    public int ProdutoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal QuantidadeEstoque { get; set; }
    public decimal PrecoVenda { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
