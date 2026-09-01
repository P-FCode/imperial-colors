namespace ImperialColors.Domain.ReadModels;

/// <summary>
/// Contadores agregados de notas fiscais (NF-e + NFC-e somadas) para o painel-resumo do hub
/// — calculados com agregação no banco (GroupBy/Sum condicionais), nunca trazendo as notas
/// para a memória para somar ali. Mesmo raciocínio de <see cref="ResumoVendasDiario"/>: o
/// volume de notas fiscais de uma única loja nunca chega a ser um problema de performance,
/// mas materializar entidades completas (com Itens/Pagamentos/Eventos incluídos, como
/// <c>NotaFiscalRepository.Consulta</c> faz para as telas de edição) só para contar linhas
/// seria puro desperdício de round-trip.
/// </summary>
public class EstatisticasNotasFiscais
{
    public int TotalEmitidas { get; set; }
    public decimal ValorTotalEmitido { get; set; }

    public int EmitidasHoje { get; set; }
    public decimal ValorEmitidoHoje { get; set; }

    public int EmitidasNoMes { get; set; }
    public decimal ValorEmitidoNoMes { get; set; }

    public int TotalCanceladas { get; set; }
    public int TotalRejeitadas { get; set; }
    public int TotalDenegadas { get; set; }

    /// <summary>Rascunho + Indeterminada: ainda não é um documento fiscal válido, mas também
    /// não foi definitivamente descartado — exige uma decisão do operador (emitir, corrigir
    /// ou consultar o status na SEFAZ).</summary>
    public int TotalPendentes { get; set; }

    public int TotalNFe { get; set; }
    public decimal ValorNFe { get; set; }
    public int TotalNFCe { get; set; }
    public decimal ValorNFCe { get; set; }

    /// <summary>Nulo quando não há nenhuma nota autorizada ainda — 0 dividido por 0 é
    /// indefinido, não zero, e a tela precisa diferenciar "sem dado" de "ticket zero".</summary>
    public decimal? TicketMedio => TotalEmitidas > 0 ? ValorTotalEmitido / TotalEmitidas : null;
}
