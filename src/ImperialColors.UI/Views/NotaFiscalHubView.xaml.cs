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

            TxtTotalEmitidas.Text = resumo.TotalEmitidas.ToString();
            TxtTotalCanceladas.Text = resumo.TotalCanceladas.ToString();
            TxtValorTotalEmitido.Text = FormattingHelper.FormatarMoeda(resumo.ValorTotalEmitido);

            GridUltimasNotas.ItemsSource = resumo.UltimasNotas.Select(n => new NotaFiscalResumoUi(n)).ToList();
            TxtSemNotas.Visibility = resumo.UltimasNotas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao carregar resumo de notas",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        public string VNfFormatado => FormattingHelper.FormatarMoeda(Origem.VNf);
    }
}
