using ClosedXML.Excel;

using ImperialColors.Application.Configuration;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;

using iText.IO.Font.Constants;

using iText.Kernel.Colors;

using iText.Kernel.Font;

using iText.Kernel.Pdf;

using iText.Layout;

using iText.Layout.Element;

using iText.Layout.Properties;

using Microsoft.Extensions.Options;

using System.IO;



namespace ImperialColors.UI.Services;



using ITextParagraph = iText.Layout.Element.Paragraph;

using ITextTable = iText.Layout.Element.Table;

using ITextCell = iText.Layout.Element.Cell;



public class RelatorioService : IRelatorioService

{

    private readonly IAppConfigService _config;

    private readonly IOptionsMonitor<EmpresaConfig> _empresa;



    public RelatorioService(IAppConfigService config, IOptionsMonitor<EmpresaConfig> empresa)

    {

        _config = config;

        _empresa = empresa;

    }



    private EmpresaConfig Empresa => _empresa.CurrentValue;



    private static PdfFont ObterFonte(bool negrito = false)

        => PdfFontFactory.CreateFont(negrito ? StandardFonts.HELVETICA_BOLD : StandardFonts.HELVETICA);



    public Task GerarCupomPdfAsync(VendaDto venda, string caminhoArquivo)

    {

        return Task.Run(() =>

        {

            using var writer = new PdfWriter(caminhoArquivo);

            using var pdf = new PdfDocument(writer);

            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A6);

            document.SetMargins(16, 16, 16, 16);



            AdicionarCabecalhoCupom(document);



            AdicionarLinhaSeparadora(document);



            document.Add(CriarLinhaRotuloValor("Venda Nº:", venda.NumeroVenda).SetMarginBottom(5));

            document.Add(CriarLinhaRotuloValor("Data:", venda.DataVenda.ToString("dd/MM/yyyy HH:mm")).SetMarginBottom(8));



            AdicionarLinhaSeparadora(document);



            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var tabela = new ITextTable(new float[] { 3.2f, 0.8f, 0.7f, 1.3f }).UseAllAvailableWidth();

            AdicionarCabecalhoTabela(tabela, "PRODUTO", "QTD", "UN", "VALOR");



            foreach (var item in venda.Itens)

            {

                tabela.AddCell(CelulaCupom(item.NomeProduto));

                tabela.AddCell(CelulaCupom(FormatarQuantidadeCupom(item.Quantidade), TextAlignment.RIGHT));

                tabela.AddCell(CelulaCupom(item.Unidade ?? "UN", TextAlignment.CENTER));

                tabela.AddCell(CelulaCupom(item.PrecoUnitario.ToString("C2", cultura), TextAlignment.RIGHT));

            }



            document.Add(tabela);

            AdicionarLinhaSeparadora(document);



            document.Add(CriarLinhaRotuloValor("Subtotal:", venda.Subtotal.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))));



            if (venda.Desconto > 0)

                document.Add(CriarLinhaRotuloValor("Desconto:", venda.Desconto.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))));

            var totalItens = venda.Itens.Sum(i => i.Quantidade);
            document.Add(CriarLinhaRotuloValor("Total de Itens:", FormatarQuantidadeCupom(totalItens)).SetMarginTop(4));

            document.Add(new ITextParagraph($"TOTAL: {venda.Total.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))}")

                .SetFont(ObterFonte(true)).SetFontSize(13).SetTextAlignment(TextAlignment.RIGHT)

                .SetFontColor(ColorConstants.BLACK).SetMarginTop(4));



            AdicionarLinhaSeparadora(document);



            document.Add(new ITextParagraph("PAGAMENTO")

                .SetFont(ObterFonte(true)).SetFontSize(10).SetFontColor(ColorConstants.BLACK).SetMarginBottom(4));



            document.Add(CriarLinhaRotuloValor("Forma:", venda.FormaPagamentoDescricao));



            if (venda.FormaPagamento == FormaPagamento.Dinheiro)

            {

                document.Add(CriarLinhaRotuloValor("Valor Recebido:", venda.ValorPago.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))));

                document.Add(CriarLinhaRotuloValor("Troco:", venda.Troco.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))));

            }



            AdicionarLinhaSeparadora(document);

            document.Add(new ITextParagraph(_config.CupomRodape)

                .SetFont(ObterFonte(true)).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER)

                .SetFontColor(ColorConstants.BLACK));

        });

    }



    private void AdicionarCabecalhoCupom(Document document)

    {

        document.Add(new ITextParagraph(Empresa.NomeFantasia.ToUpperInvariant())

            .SetFont(ObterFonte(true)).SetFontColor(ColorConstants.BLACK).SetFontSize(13)

            .SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(2));



        if (!string.IsNullOrWhiteSpace(Empresa.RazaoSocial))

        {

            document.Add(new ITextParagraph(Empresa.RazaoSocial)

                .SetFont(ObterFonte(true)).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)

                .SetFontColor(ColorConstants.BLACK).SetMarginBottom(2));

        }



        if (!string.IsNullOrWhiteSpace(Empresa.CNPJ))

        {

            document.Add(new ITextParagraph($"CNPJ: {Empresa.CNPJ}")

                .SetFont(ObterFonte(true)).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)

                .SetFontColor(ColorConstants.BLACK).SetMarginBottom(1));

        }



        if (!string.IsNullOrWhiteSpace(Empresa.Endereco))

        {

            document.Add(new ITextParagraph(Empresa.Endereco)

                .SetFont(ObterFonte(true)).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)

                .SetFontColor(ColorConstants.BLACK).SetMarginBottom(1));

        }



        if (!string.IsNullOrWhiteSpace(Empresa.Telefone))

        {

            document.Add(new ITextParagraph($"Tel: {Empresa.Telefone}")

                .SetFont(ObterFonte(true)).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER)

                .SetFontColor(ColorConstants.BLACK).SetMarginBottom(2));

        }



        document.Add(new ITextParagraph("CUPOM NÃO FISCAL")

            .SetFont(ObterFonte(true)).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER)

            .SetFontColor(ColorConstants.BLACK).SetMarginBottom(2));

    }



    private static ITextParagraph CriarLinhaRotuloValor(string rotulo, string valor)

    {

        return new ITextParagraph()

            .Add(new Text(rotulo).SetFont(ObterFonte(true)).SetFontColor(ColorConstants.BLACK))

            .Add(new Text(" ").SetFont(ObterFonte(true)).SetFontColor(ColorConstants.BLACK))

            .Add(new Text(valor).SetFont(ObterFonte(true)).SetFontColor(ColorConstants.BLACK))

            .SetFontSize(9)

            .SetMarginBottom(3)

            .SetFixedLeading(12);

    }



    private static ITextCell CelulaCupom(string texto, TextAlignment alignment = TextAlignment.LEFT)

        => new ITextCell().Add(new ITextParagraph(texto).SetFont(ObterFonte(true)).SetFontSize(8)

                .SetTextAlignment(alignment).SetFontColor(ColorConstants.BLACK))

            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)

            .SetBorderBottom(new iText.Layout.Borders.DottedBorder(ColorConstants.BLACK, 0.5f))

            .SetPadding(3);



    private static string FormatarQuantidadeCupom(decimal quantidade)

        => quantidade.ToString(quantidade % 1m == 0m ? "N0" : "N1", new System.Globalization.CultureInfo("pt-BR"));



    public Task GerarRelatorioVendasPdfAsync(IEnumerable<VendaDto> vendas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, "Relatorio de Vendas",
                $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

            var tabela = new ITextTable(new float[] { 2, 2, 3, 1.5f, 1.5f, 1.5f, 1.5f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "N Venda", "Data", "Cliente", "Subtotal", "Desconto", "Total", "Status");

            var lista = vendas.ToList();
            foreach (var venda in lista)
            {
                tabela.AddCell(CelulaTabela(venda.NumeroVenda));
                tabela.AddCell(CelulaTabela(venda.DataVenda.ToString("dd/MM/yy HH:mm")));
                tabela.AddCell(CelulaTabela(venda.ClienteNome ?? "Consumidor Final"));
                tabela.AddCell(CelulaTabela(venda.Subtotal.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(venda.Desconto.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(venda.Total.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(venda.StatusDescricao));
            }

            document.Add(tabela);

            var total = lista.Sum(v => v.Total);
            document.Add(new ITextParagraph($"\nTotal do Periodo: {total.ToString("C2", new System.Globalization.CultureInfo("pt-BR"))}")
                .SetFont(ObterFonte(true)).SetFontSize(13).SetTextAlignment(TextAlignment.RIGHT));
        });
    }

    public Task GerarRelatorioEstoquePdfAsync(IEnumerable<ProdutoDto> produtos, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, "Relatorio de Estoque", $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}");

            var tabela = new ITextTable(new float[] { 1.5f, 3, 2, 1.5f, 1, 1.5f, 1.5f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "Codigo", "Nome", "Categoria", "Marca", "Estoque", "Un", "Preco");

            foreach (var produto in produtos)
            {
                tabela.AddCell(CelulaTabela(produto.CodigoInterno));
                tabela.AddCell(CelulaTabela(produto.Nome));
                tabela.AddCell(CelulaTabela(produto.CategoriaNome ?? "-"));
                tabela.AddCell(CelulaTabela(produto.MarcaNome ?? "-"));
                var celEstoque = CelulaTabela(produto.QuantidadeEstoque.ToString("G"), TextAlignment.RIGHT);
                if (produto.SemEstoque) celEstoque.SetFontColor(new DeviceRgb(220, 53, 69));
                else if (produto.EstoqueBaixo) celEstoque.SetFontColor(new DeviceRgb(253, 126, 20));
                tabela.AddCell(celEstoque);
                tabela.AddCell(CelulaTabela(produto.Unidade));
                tabela.AddCell(CelulaTabela(produto.PrecoVenda.ToString("C2", new System.Globalization.CultureInfo("pt-BR")), TextAlignment.RIGHT));
            }

            document.Add(tabela);
            document.Add(new ITextParagraph($"\nTotal de produtos: {produtos.Count()}").SetFont(ObterFonte()).SetFontSize(11));
        });
    }

    public Task GerarRelatorioVendasExcelAsync(IEnumerable<VendaDto> vendas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Vendas");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - Relatorio de Vendas";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 7).Merge();

            ws.Cell(2, 1).Value = $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 7).Merge();

            var headers = new[] { "N Venda", "Data", "Cliente", "Subtotal", "Desconto", "Total", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            int row = 5;
            foreach (var venda in vendas)
            {
                ws.Cell(row, 1).Value = venda.NumeroVenda;
                ws.Cell(row, 2).Value = venda.DataVenda.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 3).Value = venda.ClienteNome ?? "Consumidor Final";
                ws.Cell(row, 4).Value = venda.Subtotal;
                ws.Cell(row, 4).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 5).Value = venda.Desconto;
                ws.Cell(row, 5).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 6).Value = venda.Total;
                ws.Cell(row, 6).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 7).Value = venda.StatusDescricao;
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarRelatorioEstoqueExcelAsync(IEnumerable<ProdutoDto> produtos, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Estoque");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - Relatorio de Estoque";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 7).Merge();

            var headers = new[] { "Codigo", "Nome", "Categoria", "Marca", "Estoque", "Unidade", "Preco Venda" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            int row = 4;
            foreach (var p in produtos)
            {
                ws.Cell(row, 1).Value = p.CodigoInterno;
                ws.Cell(row, 2).Value = p.Nome;
                ws.Cell(row, 3).Value = p.CategoriaNome ?? "-";
                ws.Cell(row, 4).Value = p.MarcaNome ?? "-";
                ws.Cell(row, 5).Value = p.QuantidadeEstoque;
                ws.Cell(row, 6).Value = p.Unidade;
                ws.Cell(row, 7).Value = p.PrecoVenda;
                ws.Cell(row, 7).Style.NumberFormat.Format = "R$ #,##0.00";

                if (p.SemEstoque) ws.Cell(row, 5).Style.Font.FontColor = XLColor.Red;
                else if (p.EstoqueBaixo) ws.Cell(row, 5).Style.Font.FontColor = XLColor.Orange;

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarTabelaPrecosPdfAsync(
        IEnumerable<ProdutoDto> produtos,
        string titulo,
        string subtitulo,
        decimal acrescimoPercentual,
        string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, titulo, subtitulo);

            var tabela = new ITextTable(new float[] { 2.2f, 4.5f, 2f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "CODIGO DE BARRAS", "NOME DO PRODUTO", "PRECO DE VENDA ATUAL");

            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var listaOrdenada = BinarySearchCollectionHelper.OrdenarPorId(produtos, p => p.Id);
            foreach (var produto in listaOrdenada.OrderBy(p => p.Nome))
            {
                var preco = TabelaPrecosHelper.CalcularPrecoExibicao(produto.PrecoVenda, acrescimoPercentual);
                tabela.AddCell(CelulaTabela(TabelaPrecosHelper.ObterCodigoBarrasExibicao(produto.CodigoBarras, produto.CodigoInterno)));
                tabela.AddCell(CelulaTabela(produto.NomeExibicao));
                tabela.AddCell(CelulaTabela(preco.ToString("C2", cultura), TextAlignment.RIGHT));
            }

            document.Add(tabela);
            document.Add(new ITextParagraph($"\nTotal de produtos: {produtos.Count()}")
                .SetFont(ObterFonte()).SetFontSize(11));
        });
    }

    public Task GerarTabelaPrecosExcelAsync(
        IEnumerable<ProdutoDto> produtos,
        string titulo,
        decimal acrescimoPercentual,
        string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Tabela de Precos");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - {titulo}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 3).Merge();

            ws.Cell(2, 1).Value = $"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 3).Merge();

            var headers = new[] { "CODIGO DE BARRAS", "NOME DO PRODUTO", "PRECO DE VENDA ATUAL" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            int row = 5;
            var listaOrdenada = BinarySearchCollectionHelper.OrdenarPorId(produtos, p => p.Id);
            foreach (var produto in listaOrdenada.OrderBy(p => p.Nome))
            {
                var preco = TabelaPrecosHelper.CalcularPrecoExibicao(produto.PrecoVenda, acrescimoPercentual);
                ws.Cell(row, 1).Value = TabelaPrecosHelper.ObterCodigoBarrasExibicao(produto.CodigoBarras, produto.CodigoInterno);
                ws.Cell(row, 2).Value = produto.NomeExibicao;
                ws.Cell(row, 3).Value = preco;
                ws.Cell(row, 3).Style.NumberFormat.Format = "R$ #,##0.00";

                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarRelatorioVendasConsolidadasPdfAsync(
        IEnumerable<LinhaRelatorioVendaConsolidadaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, "Relatorio Consolidado de Vendas (Geral)",
                $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} — Balcao + Vendas Externas");

            var tabela = new ITextTable(new float[] { 1.6f, 1.2f, 1.5f, 2.5f, 0.8f, 1.2f, 1f, 1.2f, 1.5f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "Data", "Origem", "Cod. Venda", "Cliente/Resumo", "Itens", "Subtotal", "Desconto", "Total", "Pagamento");

            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var lista = linhas.ToList();
            foreach (var linha in lista)
            {
                tabela.AddCell(CelulaTabela(linha.DataVenda.ToString("dd/MM/yyyy HH:mm")));
                tabela.AddCell(CelulaTabela(linha.Origem));
                tabela.AddCell(CelulaTabela(linha.NumeroVenda));
                tabela.AddCell(CelulaTabela(linha.ClienteOuResumo));
                tabela.AddCell(CelulaTabela(linha.TotalItens.ToString(), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.Subtotal.ToString("C2", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.Desconto.ToString("C2", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.Total.ToString("C2", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.FormaPagamento ?? "—"));
            }

            document.Add(tabela);
            var totalGeral = lista.Sum(l => l.Total);
            var totalBalcao = lista.Where(l => l.Origem == "Balcão").Sum(l => l.Total);
            var totalExterna = lista.Where(l => l.Origem == "Externa").Sum(l => l.Total);
            document.Add(new ITextParagraph(
                    $"\nTotal Geral: {totalGeral.ToString("C2", cultura)} | Balcao: {totalBalcao.ToString("C2", cultura)} | Externa: {totalExterna.ToString("C2", cultura)} | {lista.Count} venda(s)")
                .SetFont(ObterFonte(true)).SetFontSize(12).SetTextAlignment(TextAlignment.RIGHT));
        });
    }

    public Task GerarRelatorioVendasConsolidadasExcelAsync(
        IEnumerable<LinhaRelatorioVendaConsolidadaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Vendas Consolidadas");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - Relatorio Consolidado de Vendas (Geral)";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 9).Merge();

            ws.Cell(2, 1).Value = $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} — Balcao + Vendas Externas";
            ws.Range(2, 1, 2, 9).Merge();

            var headers = new[] { "Data", "Origem", "Cod. Venda", "Cliente/Resumo", "Itens", "Subtotal", "Desconto", "Total", "Pagamento" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            var row = 5;
            var lista = linhas.ToList();
            foreach (var linha in lista)
            {
                ws.Cell(row, 1).Value = linha.DataVenda.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 2).Value = linha.Origem;
                ws.Cell(row, 3).Value = linha.NumeroVenda;
                ws.Cell(row, 4).Value = linha.ClienteOuResumo;
                ws.Cell(row, 5).Value = linha.TotalItens;
                ws.Cell(row, 6).Value = linha.Subtotal;
                ws.Cell(row, 6).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 7).Value = linha.Desconto;
                ws.Cell(row, 7).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 8).Value = linha.Total;
                ws.Cell(row, 8).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 9).Value = linha.FormaPagamento ?? "—";
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            var totalRow = row + 1;
            ws.Cell(totalRow, 7).Value = "Total Geral:";
            ws.Cell(totalRow, 7).Style.Font.Bold = true;
            ws.Cell(totalRow, 8).Value = lista.Sum(l => l.Total);
            ws.Cell(totalRow, 8).Style.NumberFormat.Format = "R$ #,##0.00";
            ws.Cell(totalRow, 8).Style.Font.Bold = true;

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarRelatorioVendasExternasPdfAsync(
        IEnumerable<LinhaRelatorioVendaExternaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, "Relatorio de Vendas Externas",
                $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}");

            var tabela = new ITextTable(new float[] { 1.8f, 1.8f, 3f, 1.2f, 1.5f, 1.5f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "Data Venda", "Cod. Venda", "Produto/Item", "Qtd", "Vlr Unit.", "Vlr Total");

            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var lista = linhas.ToList();
            foreach (var linha in lista)
            {
                tabela.AddCell(CelulaTabela(linha.DataVenda.ToString("dd/MM/yyyy HH:mm")));
                tabela.AddCell(CelulaTabela(linha.CodigoVenda));
                tabela.AddCell(CelulaTabela(linha.ProdutoItem));
                tabela.AddCell(CelulaTabela(linha.QuantidadeVendida.ToString("G", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.ValorUnitario.ToString("C2", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(linha.ValorTotal.ToString("C2", cultura), TextAlignment.RIGHT));
            }

            document.Add(tabela);
            var total = lista.Sum(l => l.ValorTotal);
            document.Add(new ITextParagraph($"\nTotal do Periodo: {total.ToString("C2", cultura)} | {lista.Count} linha(s)")
                .SetFont(ObterFonte(true)).SetFontSize(13).SetTextAlignment(TextAlignment.RIGHT));
        });
    }

    public Task GerarRelatorioVendasExternasExcelAsync(
        IEnumerable<LinhaRelatorioVendaExternaDto> linhas, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Vendas Externas");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - Relatorio de Vendas Externas";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 6).Merge();

            ws.Cell(2, 1).Value = $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 6).Merge();

            var headers = new[] { "Data Venda", "Cod. Venda", "Produto/Item", "Qtd Vendida", "Valor Unitario", "Valor Total" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            var row = 5;
            foreach (var linha in linhas)
            {
                ws.Cell(row, 1).Value = linha.DataVenda.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(row, 2).Value = linha.CodigoVenda;
                ws.Cell(row, 3).Value = linha.ProdutoItem;
                ws.Cell(row, 4).Value = linha.QuantidadeVendida;
                ws.Cell(row, 5).Value = linha.ValorUnitario;
                ws.Cell(row, 5).Style.NumberFormat.Format = "R$ #,##0.00";
                ws.Cell(row, 6).Value = linha.ValorTotal;
                ws.Cell(row, 6).Style.NumberFormat.Format = "R$ #,##0.00";
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarRelatorioRankingProdutosPdfAsync(
        IEnumerable<ProdutoRankingDto> ranking, string titulo, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, titulo, $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} — Balcao + Vendas Externas");

            var tabela = new ITextTable(new float[] { 0.8f, 1.5f, 3.5f, 1.5f, 2f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "Pos.", "Codigo", "Nome do Produto", "Unidades", "Faturamento");

            var cultura = new System.Globalization.CultureInfo("pt-BR");
            foreach (var item in ranking)
            {
                tabela.AddCell(CelulaTabela(item.Posicao.ToString(), TextAlignment.CENTER));
                tabela.AddCell(CelulaTabela(item.CodigoInterno));
                tabela.AddCell(CelulaTabela(item.NomeProduto));
                tabela.AddCell(CelulaTabela(item.QuantidadeTotal.ToString("G", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(item.FaturamentoGerado.ToString("C2", cultura), TextAlignment.RIGHT));
            }

            document.Add(tabela);
            document.Add(new ITextParagraph($"\nProdutos listados: {ranking.Count()}")
                .SetFont(ObterFonte()).SetFontSize(11));
        });
    }

    public Task GerarRelatorioRankingProdutosExcelAsync(
        IEnumerable<ProdutoRankingDto> ranking, string titulo, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Ranking");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - {titulo}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 5).Merge();

            ws.Cell(2, 1).Value = $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 5).Merge();

            var headers = new[] { "Posicao", "Codigo", "Nome do Produto", "Total Unidades Vendidas", "Faturamento Gerado" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            var row = 5;
            foreach (var item in ranking)
            {
                ws.Cell(row, 1).Value = item.Posicao;
                ws.Cell(row, 2).Value = item.CodigoInterno;
                ws.Cell(row, 3).Value = item.NomeProduto;
                ws.Cell(row, 4).Value = item.QuantidadeTotal;
                ws.Cell(row, 5).Value = item.FaturamentoGerado;
                ws.Cell(row, 5).Style.NumberFormat.Format = "R$ #,##0.00";
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    public Task GerarRelatorioProdutosEncalhadosPdfAsync(
        IEnumerable<ProdutoEncalhadoDto> produtos, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var writer = new PdfWriter(caminhoArquivo);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(30, 30, 30, 30);

            AdicionarCabecalhoRelatorio(document, "Produtos Nunca Vendidos (Encalhados)",
                $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy} — Estoque com saldo e zero vendas");

            var tabela = new ITextTable(new float[] { 1.5f, 4f, 1.5f, 2f }).UseAllAvailableWidth();
            AdicionarCabecalhoTabela(tabela, "Codigo", "Nome do Produto", "Estoque Atual", "Valor Parado");

            var cultura = new System.Globalization.CultureInfo("pt-BR");
            var lista = produtos.ToList();
            foreach (var item in lista)
            {
                tabela.AddCell(CelulaTabela(item.CodigoInterno));
                tabela.AddCell(CelulaTabela(item.NomeProduto));
                tabela.AddCell(CelulaTabela(item.EstoqueAtual.ToString("G", cultura), TextAlignment.RIGHT));
                tabela.AddCell(CelulaTabela(item.ValorTotalParado.ToString("C2", cultura), TextAlignment.RIGHT));
            }

            document.Add(tabela);
            var totalParado = lista.Sum(p => p.ValorTotalParado);
            document.Add(new ITextParagraph($"\nCapital parado estimado: {totalParado.ToString("C2", cultura)} | {lista.Count} produto(s)")
                .SetFont(ObterFonte(true)).SetFontSize(13).SetTextAlignment(TextAlignment.RIGHT));
        });
    }

    public Task GerarRelatorioProdutosEncalhadosExcelAsync(
        IEnumerable<ProdutoEncalhadoDto> produtos, DateTime inicio, DateTime fim, string caminhoArquivo)
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Encalhados");

            ws.Cell(1, 1).Value = $"{_config.EmpresaNome} - Produtos Nunca Vendidos";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Range(1, 1, 1, 4).Merge();

            ws.Cell(2, 1).Value = $"Periodo: {inicio:dd/MM/yyyy} a {fim:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 4).Merge();

            var headers = new[] { "Codigo", "Nome do Produto", "Estoque Atual", "Valor Total Parado" };
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 194, 0);
                cell.Style.Font.Bold = true;
            }

            var row = 5;
            foreach (var item in produtos)
            {
                ws.Cell(row, 1).Value = item.CodigoInterno;
                ws.Cell(row, 2).Value = item.NomeProduto;
                ws.Cell(row, 3).Value = item.EstoqueAtual;
                ws.Cell(row, 4).Value = item.ValorTotalParado;
                ws.Cell(row, 4).Style.NumberFormat.Format = "R$ #,##0.00";
                if (row % 2 == 0)
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(248, 249, 250);
                row++;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(caminhoArquivo);
        });
    }

    private void AdicionarCabecalhoRelatorio(Document document, string titulo, string subtitulo)
    {
        document.Add(new ITextParagraph(_config.EmpresaNome.ToUpperInvariant()).SetFont(ObterFonte(true)).SetFontSize(18)
            .SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER));
        document.Add(new ITextParagraph(_config.EmpresaSubtitulo).SetFont(ObterFonte()).SetFontSize(11)
            .SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER));
        document.Add(new ITextParagraph(titulo).SetFont(ObterFonte(true)).SetFontSize(14)
            .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(10));
        document.Add(new ITextParagraph(subtitulo).SetFont(ObterFonte()).SetFontSize(10)
            .SetFontColor(ColorConstants.BLACK).SetTextAlignment(TextAlignment.CENTER).SetMarginBottom(15));

        var separador = new ITextTable(1).UseAllAvailableWidth();
        separador.AddCell(new ITextCell().SetHeight(2).SetBackgroundColor(new DeviceRgb(245, 194, 0))
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        document.Add((IBlockElement)separador.SetMarginBottom(15));
    }

    private static void AdicionarCabecalhoTabela(ITextTable tabela, params string[] headers)
    {
        foreach (var header in headers)
        {
            tabela.AddHeaderCell(new ITextCell().Add(new ITextParagraph(header).SetFont(ObterFonte(true)).SetFontSize(9))
                .SetBackgroundColor(new DeviceRgb(245, 194, 0))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(5));
        }
    }

    private static ITextCell CelulaTabela(string texto, TextAlignment alignment = TextAlignment.LEFT)
        => new ITextCell().Add(new ITextParagraph(texto).SetFont(ObterFonte()).SetFontSize(9).SetTextAlignment(alignment))
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetBorderBottom(new iText.Layout.Borders.SolidBorder(new DeviceRgb(233, 236, 239), 0.5f))
            .SetPadding(4);

    private static void AdicionarLinhaSeparadora(Document document)
    {
        var tabela = new ITextTable(1).UseAllAvailableWidth().SetMarginTop(3).SetMarginBottom(3);
        tabela.AddCell(new ITextCell().SetHeight(0.5f).SetBackgroundColor(new DeviceRgb(233, 236, 239))
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        document.Add((IBlockElement)tabela);
    }
}
