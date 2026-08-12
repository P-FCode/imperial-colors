using System.Globalization;

namespace ImperialColors.Application.DTOs;

public class DashboardDto
{
    public decimal TotalVendasHoje { get; set; }
    public decimal TotalVendasMes { get; set; }
    public int QuantidadeVendasHoje { get; set; }
    public int ProdutosEstoqueCritico { get; set; }
    public int ProdutosSemEstoque { get; set; }
    public int TotalProdutos { get; set; }

    // --- Controle financeiro: lucro, custo e margem ---

    public decimal LucroHoje { get; set; }
    public decimal LucroMes { get; set; }
    public decimal CustoHoje { get; set; }
    public decimal CustoMes { get; set; }

    /// <summary>Margem de lucro do dia, em percentual (0–100). 0 quando não houve
    /// faturamento no período (evita divisão por zero).</summary>
    public decimal MargemLucroHoje { get; set; }

    /// <summary>Margem de lucro do mês, em percentual (0–100).</summary>
    public decimal MargemLucroMes { get; set; }

    public decimal TicketMedioMes { get; set; }
    public int QuantidadeVendasMes { get; set; }

    /// <summary>Quantos itens vendidos no mês pertencem a produtos sem "Custo" cadastrado —
    /// esses itens entram no faturamento mas não no cálculo de custo/lucro, então o lucro
    /// exibido pode estar SUPERESTIMADO quando esse número não é zero. A UI usa este campo
    /// para avisar o operador, em vez de fingir que o lucro é exato.</summary>
    public int ItensSemCustoCadastradoMes { get; set; }

    public List<LucroDiarioDto> LucroUltimos7Dias { get; set; } = new();
}

/// <summary>Um dia do mini-painel "Lucro dos últimos 7 dias" do dashboard.</summary>
public class LucroDiarioDto
{
    public DateTime Data { get; set; }
    public decimal Faturamento { get; set; }
    public decimal Custo { get; set; }
    public decimal Lucro { get; set; }

    /// <summary>0–100, proporcional ao faturamento do maior dia do período — usado só para
    /// desenhar a largura da barra horizontal no dashboard (ver <c>PercentualParaLarguraConverter</c>).</summary>
    public decimal PercentualBarra { get; set; }

    public string DiaSemanaAbreviado => Data.ToString("ddd", new CultureInfo("pt-BR")).ToUpperInvariant().TrimEnd('.');
    public string DataFormatada => Data.ToString("dd/MM");
}
