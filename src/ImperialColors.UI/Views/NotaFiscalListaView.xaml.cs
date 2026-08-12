using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class NotaFiscalListaView : UserControl
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotaFiscalService _notaFiscalService;
    private readonly TipoNotaFiscal _tipo;

    // Evita que o SelectedIndex=0 setado no construtor (abaixo) dispare um primeiro
    // carregamento redundante além do que o evento Loaded já faz.
    private bool _pronto;

    public event EventHandler? VoltarSolicitado;

    public NotaFiscalListaView(IServiceProvider serviceProvider, TipoNotaFiscal tipo)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _notaFiscalService = serviceProvider.GetRequiredService<INotaFiscalService>();
        _tipo = tipo;

        CmbFiltroStatus.Items.Add(new ComboBoxItem { Content = "Todas", Tag = null });
        foreach (var status in Enum.GetValues<StatusNotaFiscal>())
            CmbFiltroStatus.Items.Add(new ComboBoxItem { Content = NotaFiscalStatusHelper.Descricao(status), Tag = status });
        CmbFiltroStatus.SelectedIndex = 0;

        Loaded += async (_, _) =>
        {
            _pronto = true;
            await CarregarAsync();
        };
    }

    private async Task CarregarAsync()
    {
        var filtroStatus = (CmbFiltroStatus.SelectedItem as ComboBoxItem)?.Tag as StatusNotaFiscal?;

        try
        {
            var notas = await _notaFiscalService.ListarAsync(_tipo, filtroStatus);
            GridNotas.ItemsSource = notas.Select(n => new NotaFiscalResumoUi(n)).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao carregar notas",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnAtualizar_Click(object sender, RoutedEventArgs e) => await CarregarAsync();

    private async void CmbFiltroStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_pronto) return;
        await CarregarAsync();
    }

    private void BtnVoltar_Click(object sender, RoutedEventArgs e) => VoltarSolicitado?.Invoke(this, EventArgs.Empty);

    private void GridNotas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnAcoesDaNota.IsEnabled = GridNotas.SelectedItem is NotaFiscalResumoUi;
        BtnExcluirNota.IsEnabled = GridNotas.SelectedItem is NotaFiscalResumoUi item &&
            item.Origem.Status is StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada;
    }

    private async void BtnExcluirNota_Click(object sender, RoutedEventArgs e)
    {
        if (GridNotas.SelectedItem is not NotaFiscalResumoUi item) return;

        if (MessageBox.Show(
                $"Excluir a nota {item.SerieNumero} ({item.StatusFormatado})? Esta ação não pode ser desfeita.",
                "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            await _notaFiscalService.ExcluirAsync(item.Origem.Id);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao excluir nota",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnCriarNota_Click(object sender, RoutedEventArgs e)
    {
        var form = new NotaFiscalFormView(_serviceProvider, _tipo, notaFiscalId: null);
        if (form.ShowDialog() == true)
            await CarregarAsync();
    }

    private async void GridNotas_MouseDoubleClick(object sender, MouseButtonEventArgs e) => await AbrirSelecionadaAsync();

    private async void BtnAcoesDaNota_Click(object sender, RoutedEventArgs e) => await AbrirAcoesDaNotaAsync();

    private async Task AbrirSelecionadaAsync()
    {
        if (GridNotas.SelectedItem is not NotaFiscalResumoUi item) return;

        if (item.Origem.Status is StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada)
        {
            var form = new NotaFiscalFormView(_serviceProvider, _tipo, item.Origem.Id);
            if (form.ShowDialog() == true)
                await CarregarAsync();
        }
        else
        {
            await AbrirAcoesDaNotaAsync();
        }
    }

    private async Task AbrirAcoesDaNotaAsync()
    {
        if (GridNotas.SelectedItem is not NotaFiscalResumoUi item) return;

        var acoes = new NotaFiscalAcoesView(_serviceProvider, item.Origem.Id);
        acoes.ShowDialog();
        await CarregarAsync();
    }

    private sealed class NotaFiscalResumoUi
    {
        public NotaFiscalResumoUi(NotaFiscalResumoDto origem) => Origem = origem;

        public NotaFiscalResumoDto Origem { get; }
        public string SerieNumero => $"{Origem.Serie}/{Origem.Numero}";
        public string DataEmissaoFormatada => FormattingHelper.FormatarDataHora(Origem.DataEmissao);
        public string StatusFormatado => NotaFiscalStatusHelper.Descricao(Origem.Status);
        public string VNfFormatado => FormattingHelper.FormatarMoeda(Origem.VNf);
        public string ClienteNome => Origem.ClienteNome ?? "(consumidor não identificado)";
        public string? ChaveAcesso => Origem.ChaveAcesso;
    }
}
