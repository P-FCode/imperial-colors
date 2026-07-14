using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImperialColors.UI.Views;

public partial class RelatoriosView : UserControl
{
    private readonly IServiceProvider _serviceProvider;
    private Button? _navAtivo;
    private string _tipoRelatorio = "VendasPeriodo";

    private static readonly Dictionary<string, (string Titulo, string Descricao, string Colunas, bool PrecisaPeriodo, bool PrecisaGiro)> Metadados = new()
    {
        ["VendasPeriodo"] = (
            "Vendas por Período",
            "Consolidado de vendas finalizadas no balcão dentro do intervalo selecionado.",
            "Data, código da venda, cliente, forma de pagamento, itens e totais.",
            true, false),
        ["VendasExternas"] = (
            "Relatório de Vendas Externas",
            "Auditoria detalhada por item das vendas de rua registradas no período.",
            "Data da venda, código, produto/item, quantidade, valor unitário e valor total.",
            true, false),
        ["VendasConsolidadas"] = (
            "Relatório Consolidado de Vendas (Geral)",
            "Unifica vendas de balcão (PDV) e vendas externas em uma única listagem filtrada por período.",
            "Data, origem (Balcão ou Externa), código da venda, cliente/resumo, itens, subtotal, desconto, total e forma de pagamento.",
            true, false),
        ["EstoqueCompleto"] = (
            "Estoque Completo",
            "Inventário de todos os produtos ativos com saldo, categoria e preço de venda.",
            "Código, nome, categoria, unidade, estoque, estoque mínimo e preço.",
            false, false),
        ["EstoqueBaixo"] = (
            "Estoque Baixo",
            "Produtos ativos com quantidade abaixo do estoque mínimo configurado.",
            "Código, nome, estoque atual, estoque mínimo e diferença.",
            false, false),
        ["SemEstoque"] = (
            "Sem Estoque",
            "Produtos ativos com quantidade zerada — itens que precisam de reposição.",
            "Código, nome, categoria e preço de venda.",
            false, false),
        ["TabelaLoja"] = (
            "Tabela de Vendas da Loja",
            "Catálogo de preços de balcão para consulta ou impressão.",
            "Código de barras, nome do produto e preço atual de venda.",
            false, false),
        ["TabelaPintor"] = (
            "Tabela do Pintor",
            "Mesma estrutura da tabela da loja, com acréscimo automático de 5% no preço exibido.",
            "Código de barras, nome do produto e preço com acréscimo para parceiros.",
            false, false),
        ["AnaliseGiro"] = (
            "Análise de Giro e Desempenho",
            "Ranking cruzando vendas de balcão e externas. Identifique campeões de venda, itens parados ou encalhados.",
            "Mais/Menos vendidos: posição, código, produto, unidades e faturamento. Encalhados: código, produto, estoque e valor parado.",
            true, true)
    };

    public RelatoriosView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;

        var inicioPadrao = DateTime.Today.AddDays(-30);
        DpInicio.SelectedDate = inicioPadrao;
        DpFim.SelectedDate = DateTime.Today;

        Loaded += (_, _) =>
        {
            DatePickerSyncHelper.SincronizarTexto(DpInicio);
            DatePickerSyncHelper.SincronizarTexto(DpFim);
            SelecionarRelatorio(BtnNavVendasPeriodo, "VendasPeriodo");
        };
    }

    private void BtnNavRelatorio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao || botao.Tag is not string tipo)
            return;

        SelecionarRelatorio(botao, tipo);
    }

    private void SelecionarRelatorio(Button botao, string tipo)
    {
        if (_navAtivo != null)
            NavMenuHelper.SetIsActive(_navAtivo, false);

        _navAtivo = botao;
        _tipoRelatorio = tipo;
        NavMenuHelper.SetIsActive(botao, true);
        AtualizarPainelDetalhe();
    }

    private void AtualizarPainelDetalhe()
    {
        if (!Metadados.TryGetValue(_tipoRelatorio, out var meta))
            return;

        TxtTituloRelatorio.Text = meta.Titulo;
        TxtDescricaoRelatorio.Text = meta.Descricao;
        TxtColunasRelatorio.Text = meta.Colunas;
        PainelPeriodo.Visibility = meta.PrecisaPeriodo ? Visibility.Visible : Visibility.Collapsed;
        PainelGiro.Visibility = meta.PrecisaGiro ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BtnFormato_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton clicado)
            return;

        BtnFormatoPdf.IsChecked = clicado == BtnFormatoPdf;
        BtnFormatoExcel.IsChecked = clicado == BtnFormatoExcel;
    }

    private bool ExportarExcel => BtnFormatoExcel.IsChecked == true;

    private static (DateTime inicio, DateTime fim) ObterPeriodo(DatePicker dpInicio, DatePicker dpFim)
    {
        var inicio = dpInicio.SelectedDate ?? DateTime.Today.AddDays(-30);
        var fim = dpFim.SelectedDate ?? DateTime.Today;
        return (inicio, fim.AddDays(1).AddSeconds(-1));
    }

    private (DateTime inicio, DateTime fim) ObterPeriodo()
        => ObterPeriodo(DpInicio, DpFim);

    private static bool TentarObterCaminhoSalvar(string nomePadrao, bool excel, out string caminho)
    {
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = nomePadrao,
            DefaultExt = excel ? ".xlsx" : ".pdf",
            Filter = excel ? "Excel|*.xlsx" : "PDF|*.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            caminho = saveDialog.FileName;
            return true;
        }

        caminho = string.Empty;
        return false;
    }

    private static void NotificarSucesso(string caminho)
        => MessageBox.Show($"Arquivo gerado com sucesso:\n{caminho}", "Relatório", MessageBoxButton.OK, MessageBoxImage.Information);

    private static void NotificarErro(Exception ex)
        => MessageBox.Show($"Erro ao gerar relatório: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

    private async void BtnGerar_Click(object sender, RoutedEventArgs e)
    {
        BtnGerar.IsEnabled = false;
        try
        {
            switch (_tipoRelatorio)
            {
                case "VendasPeriodo": await GerarVendasPeriodoAsync(); break;
                case "VendasExternas": await GerarVendasExternasAsync(); break;
                case "VendasConsolidadas": await GerarVendasConsolidadasAsync(); break;
                case "EstoqueCompleto": await GerarEstoqueAsync(p => p.ObterTodosAsync()); break;
                case "EstoqueBaixo": await GerarEstoqueAsync(p => p.ObterComEstoqueBaixoAsync()); break;
                case "SemEstoque": await GerarEstoqueAsync(p => p.ObterSemEstoqueAsync()); break;
                case "TabelaLoja": await GerarTabelaPrecosAsync(0m, "TabelaVendasLoja", "Tabela de Vendas da Loja", "Catalogo de precos de balcao"); break;
                case "TabelaPintor": await GerarTabelaPrecosAsync(TabelaPrecosHelper.AcrescimoTabelaPintorPercentual, "TabelaPintor", "Tabela do Pintor", $"Precos com acrescimo de {TabelaPrecosHelper.AcrescimoTabelaPintorPercentual:N0}%"); break;
                case "AnaliseGiro": await GerarAnaliseGiroAsync(); break;
            }
        }
        catch (Exception ex) { NotificarErro(ex); }
        finally { BtnGerar.IsEnabled = true; }
    }

    private async Task GerarVendasPeriodoAsync()
    {
        var (inicio, fim) = ObterPeriodo();
        var excel = ExportarExcel;
        if (!TentarObterCaminhoSalvar($"Vendas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}", excel, out var caminho))
            return;

        var vendas = await _serviceProvider.GetRequiredService<IVendaService>().ObterPorPeriodoAsync(inicio, fim);
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();

        if (excel)
            await relatorio.GerarRelatorioVendasExcelAsync(vendas, inicio, fim, caminho);
        else
            await relatorio.GerarRelatorioVendasPdfAsync(vendas, inicio, fim, caminho);

        NotificarSucesso(caminho);
    }

    private async Task GerarEstoqueAsync(Func<IProdutoService, Task<IEnumerable<ProdutoDto>>> obterProdutos)
    {
        var sufixo = _tipoRelatorio switch
        {
            "EstoqueBaixo" => "EstoqueBaixo",
            "SemEstoque" => "SemEstoque",
            _ => "Estoque"
        };
        var excel = ExportarExcel;
        if (!TentarObterCaminhoSalvar($"{sufixo}_{DateTime.Today:yyyyMMdd}", excel, out var caminho))
            return;

        var produtoService = _serviceProvider.GetRequiredService<IProdutoService>();
        var produtos = await obterProdutos(produtoService);
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();

        if (excel)
            await relatorio.GerarRelatorioEstoqueExcelAsync(produtos, caminho);
        else
            await relatorio.GerarRelatorioEstoquePdfAsync(produtos, caminho);

        NotificarSucesso(caminho);
    }

    private async Task GerarTabelaPrecosAsync(decimal acrescimo, string nomeArquivo, string titulo, string subtituloBase)
    {
        var excel = ExportarExcel;
        if (!TentarObterCaminhoSalvar($"{nomeArquivo}_{DateTime.Today:yyyyMMdd}", excel, out var caminho))
            return;

        var produtos = await _serviceProvider.GetRequiredService<IProdutoService>().ObterTodosAsync();
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();
        var subtitulo = $"{subtituloBase} — Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}";

        if (excel)
            await relatorio.GerarTabelaPrecosExcelAsync(produtos, titulo, acrescimo, caminho);
        else
            await relatorio.GerarTabelaPrecosPdfAsync(produtos, titulo, subtitulo, acrescimo, caminho);

        NotificarSucesso(caminho);
    }

    private async Task GerarVendasExternasAsync()
    {
        var (inicio, fim) = ObterPeriodo();
        var excel = ExportarExcel;
        if (!TentarObterCaminhoSalvar($"VendasExternas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}", excel, out var caminho))
            return;

        var analytics = _serviceProvider.GetRequiredService<IRelatorioAnalyticsService>();
        var linhas = await analytics.ObterLinhasVendasExternasAsync(inicio, fim);
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();

        if (excel)
            await relatorio.GerarRelatorioVendasExternasExcelAsync(linhas, inicio, fim, caminho);
        else
            await relatorio.GerarRelatorioVendasExternasPdfAsync(linhas, inicio, fim, caminho);

        NotificarSucesso(caminho);
    }

    private async Task GerarVendasConsolidadasAsync()
    {
        var (inicio, fim) = ObterPeriodo();
        var excel = ExportarExcel;
        if (!TentarObterCaminhoSalvar($"VendasConsolidadas_{inicio:yyyyMMdd}_{fim:yyyyMMdd}", excel, out var caminho))
            return;

        var analytics = _serviceProvider.GetRequiredService<IRelatorioAnalyticsService>();
        var linhas = await analytics.ObterVendasConsolidadasAsync(inicio, fim);
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();

        if (excel)
            await relatorio.GerarRelatorioVendasConsolidadasExcelAsync(linhas, inicio, fim, caminho);
        else
            await relatorio.GerarRelatorioVendasConsolidadasPdfAsync(linhas, inicio, fim, caminho);

        NotificarSucesso(caminho);
    }

    private async Task GerarAnaliseGiroAsync()
    {
        var (inicio, fim) = ObterPeriodo();
        var tipo = ObterTipoAnaliseGiroSelecionado();
        var excel = ExportarExcel;
        var sufixo = tipo switch
        {
            TipoAnaliseGiroProduto.MaisVendidos => "MaisVendidos",
            TipoAnaliseGiroProduto.MenosVendidos => "MenosVendidos",
            _ => "Encalhados"
        };

        if (!TentarObterCaminhoSalvar($"GiroProdutos_{sufixo}_{inicio:yyyyMMdd}_{fim:yyyyMMdd}", excel, out var caminho))
            return;

        var analytics = _serviceProvider.GetRequiredService<IRelatorioAnalyticsService>();
        var relatorio = _serviceProvider.GetRequiredService<IRelatorioService>();

        if (tipo == TipoAnaliseGiroProduto.NuncaVendidos)
        {
            var encalhados = await analytics.ObterProdutosEncalhadosAsync(inicio, fim);
            if (excel)
                await relatorio.GerarRelatorioProdutosEncalhadosExcelAsync(encalhados, inicio, fim, caminho);
            else
                await relatorio.GerarRelatorioProdutosEncalhadosPdfAsync(encalhados, inicio, fim, caminho);
        }
        else
        {
            var ranking = await analytics.ObterRankingProdutosAsync(inicio, fim, tipo);
            var titulo = tipo == TipoAnaliseGiroProduto.MaisVendidos
                ? "Produtos Mais Vendidos"
                : "Produtos Menos Vendidos";

            if (excel)
                await relatorio.GerarRelatorioRankingProdutosExcelAsync(ranking, titulo, inicio, fim, caminho);
            else
                await relatorio.GerarRelatorioRankingProdutosPdfAsync(ranking, titulo, inicio, fim, caminho);
        }

        NotificarSucesso(caminho);
    }

    private TipoAnaliseGiroProduto ObterTipoAnaliseGiroSelecionado()
    {
        var tag = (CmbTipoGiro.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return tag switch
        {
            "MenosVendidos" => TipoAnaliseGiroProduto.MenosVendidos,
            "NuncaVendidos" => TipoAnaliseGiroProduto.NuncaVendidos,
            _ => TipoAnaliseGiroProduto.MaisVendidos
        };
    }
}
