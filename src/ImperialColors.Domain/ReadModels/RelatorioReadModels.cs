namespace ImperialColors.Domain.ReadModels;

public class LinhaRelatorioVendaExternaResumo
{
    public DateTime DataVenda { get; set; }
    public string CodigoVenda { get; set; } = string.Empty;
    public string ProdutoItem { get; set; } = string.Empty;
    public decimal QuantidadeVendida { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorTotal { get; set; }
}

public class ProdutoRankingResumo
{
    public string CodigoInterno { get; set; } = string.Empty;
    public string NomeProduto { get; set; } = string.Empty;
    public decimal QuantidadeTotal { get; set; }
    public decimal FaturamentoGerado { get; set; }
}

/// <summary>
/// Faturamento e custo de UM dia, já agregados pelo banco. Existe para o dashboard não
/// precisar materializar o grafo <c>Venda → Itens → Produto</c> do mês inteiro só para somar
/// quatro cifras: um mês de 500 vendas/dia com 5 itens cada são ~112 mil linhas trazidas para
/// a memória a cada abertura do app, contra ~31 linhas deste resumo.
/// </summary>
public class ResumoVendasDiario
{
    public DateTime Data { get; set; }
    public int QuantidadeVendas { get; set; }
    public decimal Faturamento { get; set; }
    public decimal Custo { get; set; }

    /// <summary>Itens vendidos cujo produto não tem custo cadastrado — o lucro do dia está
    /// subestimado na proporção deles, e a tela avisa o operador quando é maior que zero.</summary>
    public int ItensSemCusto { get; set; }

    public decimal Lucro => Faturamento - Custo;
}

public class ProdutoEncalhadoResumo
{
    public string CodigoInterno { get; set; } = string.Empty;
    public string NomeProduto { get; set; } = string.Empty;
    public decimal EstoqueAtual { get; set; }
    public decimal ValorTotalParado { get; set; }
}
