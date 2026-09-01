using System.Windows.Media;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class NotaFiscalHubView : UserControl
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotaFiscalService _notaFiscalService;

    public NotaFiscalHubView(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _notaFiscalService = serviceProvider.GetRequiredService<INotaFiscalService>();

        Loaded += async (_, _) => await CarregarResumoAsync();
    }

    private void BtnCardNFe_Click(object sender, RoutedEventArgs e) => AbrirLista(TipoNotaFiscal.NFe);
    private void BtnCardNFCe_Click(object sender, RoutedEventArgs e) => AbrirLista(TipoNotaFiscal.NFCe);

    private void AbrirLista(TipoNotaFiscal tipo)
    {
        var painel = tipo == TipoNotaFiscal.NFe ? PainelListaNFe : PainelListaNFCe;

        if (painel.Content is null)
        {
            var lista = new NotaFiscalListaView(_serviceProvider, tipo);
            lista.VoltarSolicitado += async (_, _) => await Voltar();
            painel.Content = lista;
        }

        PainelCards.Visibility = Visibility.Collapsed;
        PainelListaNFe.Visibility = tipo == TipoNotaFiscal.NFe ? Visibility.Visible : Visibility.Collapsed;
        PainelListaNFCe.Visibility = tipo == TipoNotaFiscal.NFCe ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task Voltar()
    {
        PainelCards.Visibility = Visibility.Visible;
        PainelListaNFe.Visibility = Visibility.Collapsed;
        PainelListaNFCe.Visibility = Visibility.Collapsed;

        // O operador pode ter emitido/cancelado notas na lista antes de voltar — o resumo
        // precisa refletir isso, não ficar com os números de quando o hub abriu.
        await CarregarResumoAsync();
    }

    private async void BtnAtualizarResumo_Click(object sender, RoutedEventArgs e) => await CarregarResumoAsync();

    private async Task CarregarResumoAsync()
    {
        try
        {
            var resumo = await _notaFiscalService.ObterResumoAsync();

            PreencherTiles(resumo);
            PreencherComparativoPorTipo(resumo);
            PreencherComparativoPorStatus(resumo);

            GridUltimasNotas.ItemsSource = resumo.UltimasNotas.Select(n => new NotaFiscalResumoUi(n)).ToList();
            TxtSemNotas.Visibility = resumo.UltimasNotas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao carregar resumo de notas",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PreencherTiles(ResumoNotasFiscaisDto resumo)
    {
        TxtTotalEmitidas.Text = resumo.TotalEmitidas.ToString();
        TxtEmitidasHoje.Text = $"{resumo.EmitidasHoje} hoje";

        TxtValorTotalEmitido.Text = FormattingHelper.FormatarMoeda(resumo.ValorTotalEmitido);
        TxtValorEmitidoNoMes.Text = $"{FormattingHelper.FormatarMoeda(resumo.ValorEmitidoNoMes)} este mês";

        TxtTicketMedio.Text = resumo.TicketMedio is decimal ticket
            ? FormattingHelper.FormatarMoeda(ticket)
            : "—";

        TxtTotalPendentes.Text = resumo.TotalPendentes.ToString();
        TxtTotalRejeitadas.Text = resumo.TotalRejeitadas.ToString();
        TxtTotalCanceladas.Text = resumo.TotalCanceladas.ToString();
    }

    /// <summary>
    /// Duas barras cujo preenchimento é proporcional à participação de cada tipo nas notas
    /// AUTORIZADAS — expressas como <see cref="GridLength"/> em estrela, não em pixels: assim
    /// a barra acompanha a largura real do painel (que muda com o tamanho da janela) sem
    /// precisar medir <c>ActualWidth</c> depois do layout.
    /// </summary>
    private void PreencherComparativoPorTipo(ResumoNotasFiscaisDto resumo)
    {
        var total = resumo.TotalNFe + resumo.TotalNFCe;

        PainelBarrasTipo.Visibility = total > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtSemComparativoTipo.Visibility = total > 0 ? Visibility.Collapsed : Visibility.Visible;

        if (total == 0)
            return;

        TxtContagemNFe.Text = $"{resumo.TotalNFe} nota(s)";
        TxtValorNFe.Text = FormattingHelper.FormatarMoeda(resumo.ValorNFe);
        TxtContagemNFCe.Text = $"{resumo.TotalNFCe} nota(s)";
        TxtValorNFCe.Text = FormattingHelper.FormatarMoeda(resumo.ValorNFCe);

        DefinirProporcaoBarra(ColBarraNFePreenchida, ColBarraNFeVazia, resumo.TotalNFe, total);
        DefinirProporcaoBarra(ColBarraNFCePreenchida, ColBarraNFCeVazia, resumo.TotalNFCe, total);
    }

    /// <summary>Uma barra com participação zero ainda mostra uma lasca mínima (2%): 0 de
    /// largura deixaria a cor da barra invisível, indistinguível de "sem dado".</summary>
    private static void DefinirProporcaoBarra(ColumnDefinition preenchida, ColumnDefinition vazia, int quantidade, int total)
    {
        var proporcao = Math.Max(quantidade / (double)total, quantidade > 0 ? 0.02 : 0.0);
        preenchida.Width = new GridLength(proporcao, GridUnitType.Star);
        vazia.Width = new GridLength(1 - proporcao, GridUnitType.Star);
    }

    private void PreencherComparativoPorStatus(ResumoNotasFiscaisDto resumo)
    {
        TxtStatusAutorizadas.Text = resumo.TotalEmitidas.ToString();
        TxtStatusRejeitadas.Text = resumo.TotalRejeitadas.ToString();
        TxtStatusPendentes.Text = resumo.TotalPendentes.ToString();
        TxtStatusCanceladas.Text = resumo.TotalCanceladas.ToString();

        // Denegada é rara (irregularidade cadastral do emitente/destinatário) — mostrar uma
        // linha permanente com "0" para um status que quase nunca acontece só adicionaria
        // ruído a um painel que já tem cinco outras linhas.
        LinhaStatusDenegadas.Visibility = resumo.TotalDenegadas > 0 ? Visibility.Visible : Visibility.Collapsed;
        TxtStatusDenegadas.Text = resumo.TotalDenegadas.ToString();
    }

    private async void GridUltimasNotas_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (GridUltimasNotas.SelectedItem is not NotaFiscalResumoUi item) return;

        if (item.Origem.Status is StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada)
        {
            var form = new NotaFiscalFormView(_serviceProvider, item.Origem.Tipo, item.Origem.Id);
            form.ShowDialog();
        }
        else
        {
            var acoes = new NotaFiscalAcoesView(_serviceProvider, item.Origem.Id);
            acoes.ShowDialog();
        }

        await CarregarResumoAsync();
    }

    private sealed class NotaFiscalResumoUi
    {
        public NotaFiscalResumoUi(NotaFiscalResumoDto origem) => Origem = origem;

        public NotaFiscalResumoDto Origem { get; }
        public string TipoDescricao => Origem.Tipo == TipoNotaFiscal.NFCe ? "NFC-e" : "NF-e";
        public string SerieNumero => $"{Origem.Serie}/{Origem.Numero}";
        public string ClienteNome => Origem.ClienteNome ?? "(consumidor não identificado)";
        public string DataEmissaoFormatada => FormattingHelper.FormatarDataHora(Origem.DataEmissao);
        public string StatusFormatado => NotaFiscalStatusHelper.Descricao(Origem.Status);
        public Brush StatusCorFundo => NotaFiscalStatusHelper.CorFundo(Origem.Status);
        public Brush StatusCorTexto => NotaFiscalStatusHelper.CorTexto(Origem.Status);
        public string VNfFormatado => FormattingHelper.FormatarMoeda(Origem.VNf);
    }
}
