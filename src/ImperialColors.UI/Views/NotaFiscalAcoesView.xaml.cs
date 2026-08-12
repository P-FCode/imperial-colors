using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ImperialColors.UI.Views;

public partial class NotaFiscalAcoesView : Window
{
    private readonly INotaFiscalService _notaFiscalService;
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;
    private readonly ILocalConfigService _localConfigService;
    private readonly int _notaFiscalId;
    private NotaFiscalDto _nota = new();
    private string? _ufEmitente;

    public NotaFiscalAcoesView(IServiceProvider serviceProvider, int notaFiscalId)
    {
        InitializeComponent();
        _notaFiscalService = serviceProvider.GetRequiredService<INotaFiscalService>();
        _configuracaoFiscal = serviceProvider.GetRequiredService<IConfiguracaoFiscalService>();
        _localConfigService = serviceProvider.GetRequiredService<ILocalConfigService>();
        _notaFiscalId = notaFiscalId;

        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        try
        {
            _nota = await _notaFiscalService.ObterPorIdAsync(_notaFiscalId)
                ?? throw new DomainException("Nota fiscal não encontrada.");
            _ufEmitente = (await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync()).Uf;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        AtualizarCabecalho();
        AtualizarVisibilidadesPorTipoEStatus();
        AtualizarEventos();
    }

    /// <summary>Prefixo usado nos nomes de arquivo de XML/DANFE baixados/impressos — sem
    /// isso, o download de uma NFC-e salvava com o mesmo prefixo "NFe_" de uma NF-e,
    /// misturando os dois tipos de nota na pasta de downloads do operador.</summary>
    private string PrefixoArquivo => _nota.Tipo == TipoNotaFiscal.NFCe ? "NFCe" : "NFe";

    private void AtualizarCabecalho()
    {
        var tipoTexto = _nota.Tipo == TipoNotaFiscal.NFCe ? "NFC-e" : "NF-e";
        TxtCabecalho.Text = $"{tipoTexto} {_nota.Serie}/{_nota.Numero}";
        TxtSubCabecalho.Text = string.IsNullOrWhiteSpace(_nota.ChaveAcesso)
            ? "Nota ainda não emitida."
            : $"Chave de acesso: {_nota.ChaveAcesso}  •  Status: {_nota.Status}";

        TxtSituacao.Text = _nota.Status.ToString();
        TxtMotivoStatus.Text = _nota.XMotivo ?? _nota.MensagemErro ?? string.Empty;
        TxtChaveAcesso.Text = string.IsNullOrWhiteSpace(_nota.ChaveAcesso) ? "—" : _nota.ChaveAcesso;
    }

    private void AtualizarVisibilidadesPorTipoEStatus()
    {
        var ehNFe = _nota.Tipo == TipoNotaFiscal.NFe;
        PainelCartaCorrecao.Visibility = ehNFe ? Visibility.Visible : Visibility.Collapsed;
        PainelInutilizacao.Visibility = ehNFe ? Visibility.Visible : Visibility.Collapsed;

        var podeEmitir = _nota.Status is StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada;
        PainelEmitir.Visibility = podeEmitir ? Visibility.Visible : Visibility.Collapsed;

        var temChave = !string.IsNullOrWhiteSpace(_nota.ChaveAcesso);
        BtnConsultarStatus.IsEnabled = temChave;
        BtnVerXml.IsEnabled = temChave;
        BtnBaixarXml.IsEnabled = temChave;
        BtnBaixarDanfe.IsEnabled = temChave;

        var autorizada = _nota.Status == StatusNotaFiscal.Autorizada;
        BtnCancelarNota.IsEnabled = autorizada;
        TxtAvisoCancelamento.Text = ehNFe
            ? "Prazo: até 24h após a autorização. Mínimo 15 caracteres."
            : "Prazo: até 30 minutos após a autorização. Mínimo 15 caracteres.";
        if (!autorizada)
            TxtAvisoCancelamento.Text += " (Só é possível cancelar uma nota Autorizada.)";

        BtnImprimirDanfe.IsEnabled = autorizada;

        TxtInutSerie.Text = _nota.Serie;
    }

    private async void BtnEmitirNota_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                $"Confirma a emissão desta {(_nota.Tipo == TipoNotaFiscal.NFCe ? "NFC-e" : "NF-e")} agora, " +
                $"no ambiente de {(_nota.Ambiente == AmbienteEmissaoFiscal.Producao ? "PRODUÇÃO (valor fiscal real)" : "Homologação")}?",
                "Confirmar emissão", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        BtnEmitirNota.IsEnabled = false;
        try
        {
            _nota = await _notaFiscalService.EmitirAsync(_notaFiscalId);
            AtualizarCabecalho();
            AtualizarVisibilidadesPorTipoEStatus();
            AtualizarEventos();

            if (_nota.Status == StatusNotaFiscal.Autorizada)
            {
                MessageBox.Show(
                    $"Nota fiscal autorizada com sucesso!\n\nChave de acesso: {_nota.ChaveAcesso}\nProtocolo: {_nota.NProt}",
                    "Emissão concluída", MessageBoxButton.OK, MessageBoxImage.Information);
                MostrarMensagem("Nota autorizada.", sucesso: true);
            }
            else
            {
                MostrarMensagem($"Nota rejeitada: {_nota.XMotivo ?? _nota.MensagemErro}", sucesso: false);
            }
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
            // EmitirAsync registra o evento de falha (RegistrarEventoAsync) antes de relançar
            // a exceção — sem recarregar aqui, esse retorno da SEFAZ só aparecia no histórico
            // se o operador fechasse e reabrisse esta janela.
            await RecarregarAposFalhaAsync();
        }
        finally
        {
            BtnEmitirNota.IsEnabled = true;
        }
    }

    /// <summary>
    /// Recarrega a nota e atualiza cabeçalho/histórico após uma ação fiscal que falhou —
    /// vários fluxos (emissão, CC-e, cancelamento) gravam o evento de retorno da SEFAZ
    /// mesmo quando rejeitam e lançam exceção, então sem isso o operador só via aquele
    /// retorno na "tabelinha de ações fiscais" depois de fechar e reabrir a janela.
    /// Silenciosa de propósito: se o próprio recarregamento falhar, não deve mascarar o
    /// erro original já exibido por <see cref="MostrarMensagem"/>.
    /// </summary>
    private async Task RecarregarAposFalhaAsync()
    {
        try
        {
            var recarregada = await _notaFiscalService.ObterPorIdAsync(_notaFiscalId);
            if (recarregada is null) return;

            _nota = recarregada;
            AtualizarCabecalho();
            AtualizarVisibilidadesPorTipoEStatus();
            AtualizarEventos();
        }
        catch
        {
            // Ignorado de propósito — ver comentário no sumário do método.
        }
    }

    private void AtualizarEventos()
    {
        GridEventos.ItemsSource = _nota.Eventos.Select(e => new EventoUi(e)).ToList();
    }

    private async void BtnConsultarStatus_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _nota = await _notaFiscalService.ConsultarStatusAsync(_notaFiscalId);
            AtualizarCabecalho();
            AtualizarVisibilidadesPorTipoEStatus();
            AtualizarEventos();
            MostrarMensagem("Status atualizado.", sucesso: true);
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private async void BtnVerXml_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var xml = await _notaFiscalService.ObterXmlAsync(_notaFiscalId);
            var visualizador = new Window
            {
                Title = $"XML — {_nota.Serie}/{_nota.Numero}",
                Width = 800,
                Height = 600,
                Owner = this,
                Content = new System.Windows.Controls.TextBox
                {
                    Text = xml,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 12,
                    Padding = new Thickness(10),
                    AcceptsReturn = true
                }
            };
            visualizador.ShowDialog();
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private async void BtnBaixarXml_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var xml = await _notaFiscalService.ObterXmlAsync(_notaFiscalId);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{PrefixoArquivo}_{_nota.ChaveAcesso ?? _nota.Numero}",
                DefaultExt = ".xml",
                Filter = "XML|*.xml"
            };
            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(dialog.FileName, xml);
                MostrarMensagem($"XML salvo em:\n{dialog.FileName}", sucesso: true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private async void BtnBaixarDanfe_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pdf = await _notaFiscalService.ObterDanfeAsync(_notaFiscalId);
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"DANFE_{PrefixoArquivo}_{_nota.ChaveAcesso ?? _nota.Numero}",
                DefaultExt = ".pdf",
                Filter = "PDF|*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dialog.FileName, pdf);
                MostrarMensagem($"DANFE salvo em:\n{dialog.FileName}", sucesso: true);
            }
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private async void BtnImprimirDanfe_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pdf = await _notaFiscalService.ObterDanfeAsync(_notaFiscalId);
            var impressora = _localConfigService.ImpressoraSelecionada;

            if (!DanfePrintHelper.ImprimirNaImpressoraConfigurada(
                    pdf, $"DANFE_{PrefixoArquivo}_{_nota.ChaveAcesso ?? _nota.Numero}", impressora, out var erro))
            {
                MostrarMensagem(erro ?? "Não foi possível imprimir o DANFE.", sucesso: false);
                return;
            }

            MostrarMensagem("DANFE enviado para impressão.", sucesso: true);
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private async void BtnEnviarCorrecao_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            NotaFiscalValidator.ValidarCorrecao(TxtCorrecao.Text);
            _nota = await _notaFiscalService.CartaCorrecaoAsync(_notaFiscalId, TxtCorrecao.Text.Trim());
            TxtCorrecao.Text = string.Empty;
            AtualizarEventos();
            MostrarMensagem("Carta de correção registrada com sucesso.", sucesso: true);
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
            await RecarregarAposFalhaAsync();
        }
    }

    private async void BtnCancelarNota_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Confirma o cancelamento desta nota? Esta ação não pode ser desfeita.",
                "Confirmar cancelamento", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            NotaFiscalValidator.ValidarJustificativaCancelamento(TxtJustificativaCancelamento.Text);
            _nota = await _notaFiscalService.CancelarAsync(_notaFiscalId, TxtJustificativaCancelamento.Text.Trim());
            AtualizarCabecalho();
            AtualizarVisibilidadesPorTipoEStatus();
            AtualizarEventos();
            MostrarMensagem("Nota cancelada com sucesso.", sucesso: true);
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
            await RecarregarAposFalhaAsync();
        }
    }

    private async void BtnInutilizar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            NotaFiscalValidator.ValidarInutilizacao(TxtInutJustificativa.Text, TxtInutNumeroInicial.Text, TxtInutNumeroFinal.Text);

            var cUF = UfCodigoIbgeHelper.ObterCodigo(_ufEmitente)
                ?? throw new DomainException("UF do emitente não cadastrada (Configurações → Fiscal → Endereço Fiscal do Emitente).");

            var resultado = await _notaFiscalService.InutilizarAsync(
                cUF: cUF,
                ano: DateTime.Now.Year.ToString(),
                serie: TxtInutSerie.Text.Trim(),
                numeroInicial: TxtInutNumeroInicial.Text.Trim(),
                numeroFinal: TxtInutNumeroFinal.Text.Trim(),
                justificativa: TxtInutJustificativa.Text.Trim(),
                ambiente: _nota.Ambiente);

            if (resultado.Aprovado)
            {
                MostrarMensagem($"Inutilização homologada — faixa {resultado.Faixa}. Protocolo: {resultado.NProt}", sucesso: true);
                TxtInutJustificativa.Text = string.Empty;
            }
            else
            {
                MostrarMensagem($"Inutilização rejeitada: {resultado.XMotivo ?? resultado.Erro}", sucesso: false);
            }
        }
        catch (Exception ex)
        {
            MostrarMensagem(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();

    private void MostrarMensagem(string mensagem, bool sucesso)
    {
        TxtMensagem.Text = mensagem;
        TxtMensagem.Foreground = (Brush)FindResource(sucesso ? "VerdeSucessoBrush" : "VermelhoErroBrush");
    }

    private sealed class EventoUi
    {
        public EventoUi(NotaFiscalEventoDto origem)
        {
            DataHoraFormatada = FormattingHelper.FormatarDataHora(origem.DataHora);
            TipoDescricao = origem.Tipo.ToString();
            ResultadoDescricao = origem.Sucesso ? "Sucesso" : "Falha";
            Detalhe = origem.XMotivo ?? origem.Texto ?? string.Empty;
        }

        public string DataHoraFormatada { get; }
        public string TipoDescricao { get; }
        public string ResultadoDescricao { get; }
        public string Detalhe { get; }
    }
}
