using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

using ITextParagraph = iText.Layout.Element.Paragraph;
using ITextCell = iText.Layout.Element.Cell;
using ITextTable = iText.Layout.Element.Table;

namespace ImperialColors.UI.Services;

/// <summary>
/// Gera o PDF do Manual de Operação do Usuário.
/// </summary>
public class DocumentosPdfService
{
    private static PdfFont FonteNormal() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
    private static PdfFont FonteNegrito() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
    private static PdfFont FonteItalico() => PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

    private static readonly Color AmareloPrimario = new DeviceRgb(245, 194, 0);
    private static readonly Color PretoTexto = ColorConstants.BLACK;
    private static readonly Color CinzaTexto = new DeviceRgb(90, 90, 90);

    // ─────────────────────────────────────────────────────────────────────────────
    // MANUAL DE USO
    // ─────────────────────────────────────────────────────────────────────────────

    public Task GerarManualPdfAsync(string caminhoArquivo)
        => Task.Run(() => GerarManual(caminhoArquivo));

    private void GerarManual(string caminho)
    {
        // Primeira passagem: gera o conteúdo do PDF
        GerarConteudoManual(caminho);

        // Segunda passagem: adiciona numeração de páginas
        // É obrigatório garantir que a primeira passagem fechou TODOS os streams
        // antes de abrir o arquivo novamente — feito pelo escopo do método acima.
        AdicionarNumeracaoPaginas(caminho);
    }

    private static void GerarConteudoManual(string caminho)
    {
        // Uso de using com bloco explícito { } garante Dispose antes do retorno,
        // liberando o handle do arquivo no Windows imediatamente.
        using (var writer = new PdfWriter(caminho))
        using (var pdf = new PdfDocument(writer))
        using (var document = new Document(pdf, PageSize.A4))
        {
            document.SetMargins(55, 42, 55, 42);

            // ── Capa ──────────────────────────────────────────────────────────
            AdicionarFaixaTitulo(document,
                "Manual de Operação do Usuário",
                "Imperial Colors — Sistema de Gestão Comercial");

            document.Add(new ITextParagraph(
                    $"Data de geração: {DateTime.Now:dd/MM/yyyy}  |  Versão 1.2.0")
                .SetFont(FonteItalico()).SetFontSize(9).SetFontColor(CinzaTexto)
                .SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(22));

            // ── Seção 1 ───────────────────────────────────────────────────────
            AdicionarTituloSecao(document, "Seção 1 — Dashboard e BI");
            AdicionarTexto(document,
                "A tela de Dashboard apresenta os indicadores comerciais em tempo real da loja. " +
                "Na parte superior são exibidos os totalizadores rápidos: valor total vendido no mês, " +
                "número de vendas finalizadas, produtos com estoque baixo e produtos sem estoque.");
            AdicionarTexto(document,
                "O painel de Análise de Giro (acessível em Relatórios → Giro de Produtos) exibe três visões:");
            AdicionarLista(document, new[]
            {
                "Mais Vendidos — ranking decrescente de produtos por volume de unidades saídas (balcão + rua).",
                "Menos Vendidos — mesma lógica, ordem crescente; ideal para identificar produtos parados.",
                "Encalhados — produtos com estoque físico positivo e zero vendas no período filtrado, " +
                "acompanhados do valor total imobilizado em estoque."
            });
            AdicionarTexto(document,
                "Para ler os gráficos corretamente: ajuste o filtro de datas (Data Início / Data Fim) e " +
                "clique em \"Gerar relatório\" selecionando o formato desejado (PDF ou Excel). " +
                "O período padrão são os últimos 30 dias.");

            // ── Seção 2 ───────────────────────────────────────────────────────
            AdicionarTituloSecao(document, "Seção 2 — Controle de Estoque");
            AdicionarTexto(document,
                "Acesse Estoque no menu lateral para gerenciar o inventário completo da loja.");
            AdicionarTexto(document,
                "Para cadastrar um produto com leitor de código de barras:");
            AdicionarLista(document, new[]
            {
                "Clique em \"+ Novo Produto\" na tela de Estoque.",
                "Posicione o cursor no campo \"Código de Barras\" e passe o leitor pelo código do produto.",
                "Confirme ou ajuste os dados (Categoria, Marca, Preço de Custo e Preço de Venda) " +
                "e clique em Salvar.",
                "Se preferir, preencha o Nome manualmente — o sistema gerará o Código Interno " +
                "automaticamente com base no nome digitado."
            });
            AdicionarTexto(document,
                "Dica: utilize o filtro \"Apenas em Promoção\" na listagem para visualizar rapidamente " +
                "os produtos com preço promocional ativo.");

            // ── Seção 3 ───────────────────────────────────────────────────────
            AdicionarTituloSecao(document, "Seção 3 — Fluxo do PDV (Checkout)");
            AdicionarTexto(document,
                "O PDV é acessado pelo atalho PDV — Nova Venda (tecla F2) ou pelo menu lateral. " +
                "O processo de fechamento segue um fluxo guiado em etapas:");
            AdicionarLista(document, new[]
            {
                "Etapa 1 — Leitura de produtos: passe o leitor no código de barras ou busque pelo nome. " +
                "O item é adicionado à grade da venda com preço e unidade.",
                "Etapa 2 — Identificação do comprador (opcional): vincule um cliente cadastrado ou informe " +
                "o nome/CPF/CNPJ diretamente no cupom.",
                "Etapa 3 — Desconto: aplique desconto em valor (R$) diretamente no total.",
                "Etapa 4 — Forma de pagamento: selecione Dinheiro, Cartão Débito, Cartão Crédito, " +
                "PIX ou Boleto. Para cartão crédito, escolha o número de parcelas (1 a 12x).",
                "Etapa 5 — Confirmação: clique em \"Finalizar Venda\". O estoque é atualizado " +
                "automaticamente e o cupom não fiscal é exibido para impressão."
            });
            AdicionarTexto(document,
                "O cupom pode ser impresso diretamente na impressora configurada em " +
                "Configurações → Periféricos, ou salvo em PDF.");

            // ── Seção 4 ───────────────────────────────────────────────────────
            AdicionarTituloSecao(document, "Seção 4 — Vendas Externas");
            AdicionarTexto(document,
                "O módulo de Vendas Externas permite registrar pedidos realizados fora da loja " +
                "(vendas de rua), com ou sem conexão imediata ao estoque.");
            AdicionarTexto(document, "Fluxo de registro de uma venda externa:");
            AdicionarLista(document, new[]
            {
                "Acesse Vendas Externas no menu lateral e clique em \"+ Nova Venda Externa\".",
                "Adicione os itens manualmente (nome, quantidade e preço).",
                "Informe o nome do vendedor responsável no campo Usuário/Vendedor.",
                "Salve. O estoque é descontado automaticamente para itens vinculados ao cadastro."
            });
            AdicionarTexto(document, "Gerenciamento de trocas:");
            AdicionarLista(document, new[]
            {
                "Selecione a venda externa na listagem e clique em \"Registrar Troca\".",
                "Informe o item devolvido (produto original + quantidade) e o item substituto.",
                "O sistema realiza o estorno do produto devolvido e desconta o novo produto, " +
                "mantendo histórico auditável vinculado à venda de origem."
            });

        } // PdfWriter, PdfDocument e Document são descartados aqui — handles liberados
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // NUMERAÇÃO DE PÁGINAS (segunda passagem sobre o arquivo já fechado)
    // ─────────────────────────────────────────────────────────────────────────────

    private static void AdicionarNumeracaoPaginas(string caminho)
    {
        var tempPath = caminho + ".tmp";
        System.IO.File.Copy(caminho, tempPath, overwrite: true);

        try
        {
            // Bloco using explícito: reader, writer e pdf são descartados ao sair do bloco,
            // ANTES de File.Delete ser chamado — eliminando a trava de arquivo no Windows.
            using (var reader = new PdfReader(tempPath))
            using (var writer = new PdfWriter(caminho))
            using (var pdf = new PdfDocument(reader, writer))
            {
                var totalPaginas = pdf.GetNumberOfPages();
                var fonte = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var cinza = new DeviceRgb(150, 150, 150);

                for (var i = 1; i <= totalPaginas; i++)
                {
                    var page = pdf.GetPage(i);
                    var ps = page.GetPageSize();
                    var canvas = new PdfCanvas(page);

                    canvas.BeginText()
                          .SetFontAndSize(fonte, 8)
                          .SetColor(cinza, true)
                          .MoveText(ps.GetWidth() / 2f - 22f, 26f)
                          .ShowText($"Página {i} de {totalPaginas}")
                          .EndText()
                          .Release();
                }
            } // ← todos os streams fechados aqui
        }
        finally
        {
            // Executado com certeza, mesmo em exceção; o arquivo já foi liberado pelo bloco acima.
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────────

    private static void AdicionarFaixaTitulo(Document document, string titulo, string subtitulo)
    {
        var fundo = new ITextTable(1).UseAllAvailableWidth();
        var cell = new ITextCell()
            .SetBackgroundColor(AmareloPrimario)
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
            .SetPadding(14);

        cell.Add(new ITextParagraph(titulo)
            .SetFont(FonteNegrito()).SetFontSize(16).SetFontColor(PretoTexto)
            .SetTextAlignment(TextAlignment.CENTER));
        cell.Add(new ITextParagraph(subtitulo)
            .SetFont(FonteItalico()).SetFontSize(10).SetFontColor(PretoTexto)
            .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(4));

        fundo.AddCell(cell);
        document.Add(fundo.SetMarginBottom(18));
    }

    private static void AdicionarTituloSecao(Document document, string titulo)
    {
        document.Add(new ITextParagraph(titulo)
            .SetFont(FonteNegrito()).SetFontSize(11).SetFontColor(PretoTexto)
            .SetMarginTop(14).SetMarginBottom(5));

        var separador = new ITextTable(1).UseAllAvailableWidth();
        separador.AddCell(new ITextCell()
            .SetHeight(2f).SetBackgroundColor(AmareloPrimario)
            .SetBorder(iText.Layout.Borders.Border.NO_BORDER));
        document.Add(separador.SetMarginBottom(7));
    }

    private static void AdicionarTexto(Document document, string texto)
    {
        document.Add(new ITextParagraph(texto)
            .SetFont(FonteNormal()).SetFontSize(10).SetFontColor(PretoTexto)
            .SetMarginBottom(5).SetFixedLeading(14));
    }

    private static void AdicionarLista(Document document, IEnumerable<string> itens)
    {
        var lista = new List()
            .SetListSymbol("• ")
            .SetFont(FonteNormal())
            .SetFontSize(10)
            .SetFontColor(PretoTexto)
            .SetMarginLeft(14)
            .SetMarginBottom(6);

        foreach (var item in itens)
            lista.Add(new ListItem(item));

        document.Add(lista);
    }
}
