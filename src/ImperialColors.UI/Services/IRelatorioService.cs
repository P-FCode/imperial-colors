using ImperialColors.Application.DTOs;

namespace ImperialColors.UI.Services;

public interface IRelatorioService
{
    Task GerarCupomPdfAsync(VendaDto venda, string caminhoArquivo);
    Task GerarRelatorioVendasPdfAsync(IEnumerable<VendaDto> vendas, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioEstoquePdfAsync(IEnumerable<ProdutoDto> produtos, string caminhoArquivo);
    Task GerarRelatorioVendasExcelAsync(IEnumerable<VendaDto> vendas, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioEstoqueExcelAsync(IEnumerable<ProdutoDto> produtos, string caminhoArquivo);
    Task GerarTabelaPrecosPdfAsync(IEnumerable<ProdutoDto> produtos, string titulo, string subtitulo, decimal acrescimoPercentual, string caminhoArquivo);
    Task GerarTabelaPrecosExcelAsync(IEnumerable<ProdutoDto> produtos, string titulo, decimal acrescimoPercentual, string caminhoArquivo);

    Task GerarRelatorioVendasExternasPdfAsync(IEnumerable<LinhaRelatorioVendaExternaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioVendasExternasExcelAsync(IEnumerable<LinhaRelatorioVendaExternaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo);

    Task GerarRelatorioRankingProdutosPdfAsync(IEnumerable<ProdutoRankingDto> ranking, string titulo, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioRankingProdutosExcelAsync(IEnumerable<ProdutoRankingDto> ranking, string titulo, DateTime inicio, DateTime fim, string caminhoArquivo);

    Task GerarRelatorioProdutosEncalhadosPdfAsync(IEnumerable<ProdutoEncalhadoDto> produtos, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioProdutosEncalhadosExcelAsync(IEnumerable<ProdutoEncalhadoDto> produtos, DateTime inicio, DateTime fim, string caminhoArquivo);

    Task GerarRelatorioVendasConsolidadasPdfAsync(IEnumerable<LinhaRelatorioVendaConsolidadaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo);
    Task GerarRelatorioVendasConsolidadasExcelAsync(IEnumerable<LinhaRelatorioVendaConsolidadaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo);
}
