using System.Windows;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;

namespace ImperialColors.UI.Views;

/// <summary>
/// Fluxo de atualização do sistema: consulta a última release, mostra o que muda e, se o
/// operador confirmar, baixa o pacote e fecha o aplicativo para a troca acontecer.
///
/// A confirmação é uma etapa deliberada, não uma formalidade. Este sistema é caixa: fechar
/// sozinho no meio de uma venda seria pior que ficar desatualizado. Quem decide a hora é o
/// operador, e o texto diz exatamente o que vai acontecer com a janela aberta.
/// </summary>
public partial class AtualizacaoSistemaDialogView : Window
{
    private readonly IAtualizadorSistemaService _atualizador;
    private ResultadoVerificacaoAtualizacaoDto? _verificacao;
    private bool _atualizando;

    public AtualizacaoSistemaDialogView(IAtualizadorSistemaService atualizador)
    {
        InitializeComponent();
        _atualizador = atualizador;
        Loaded += async (_, _) => await VerificarAsync();
    }

    private async Task VerificarAsync()
    {
        MostrarMensagem("⟳", "Consultando as versões publicadas...");
        TxtSubtitulo.Text = $"Versão instalada: {_atualizador.VersaoInstaladaTexto}";

        ResultadoVerificacaoAtualizacaoDto resultado;
        try
        {
            resultado = await _atualizador.VerificarAsync();
        }
        catch (Exception ex)
        {
            MostrarMensagem("⚠", $"Não foi possível verificar atualizações.\n\n{ex.Message}", erro: true);
            return;
        }

        _verificacao = resultado;

        if (!resultado.Sucesso)
        {
            MostrarMensagem("⚠", resultado.MensagemErro ?? "Não foi possível verificar atualizações.", erro: true);
            return;
        }

        if (!resultado.AtualizacaoDisponivel)
        {
            // Uma release nova sem o pacote anexado cai aqui: não dá para atualizar, mas
            // dizer "você está atualizado" seria mentira, então a mensagem específica manda.
            if (!string.IsNullOrWhiteSpace(resultado.MensagemErro))
                MostrarMensagem("⚠", resultado.MensagemErro, erro: true);
            else
                MostrarMensagem("✓", $"O sistema já está na versão mais recente ({resultado.VersaoInstaladaTexto}).");
            return;
        }

        MostrarAtualizacaoDisponivel(resultado);
    }

    private void MostrarAtualizacaoDisponivel(ResultadoVerificacaoAtualizacaoDto resultado)
    {
        PainelMensagem.Visibility = Visibility.Collapsed;
        PainelVersoes.Visibility = Visibility.Visible;
        PainelNotas.Visibility = Visibility.Visible;
        BtnAtualizar.Visibility = Visibility.Visible;

        TxtSubtitulo.Text = "Uma nova versão está disponível para instalação.";
        TxtVersaoInstalada.Text = resultado.VersaoInstaladaTexto;
        TxtVersaoNova.Text = resultado.VersaoPublicadaTexto;
        TxtTamanho.Text = $"Download de {resultado.TamanhoTexto}";
        TxtNomeRelease.Text = resultado.NomeRelease;
        TxtNotas.Text = string.IsNullOrWhiteSpace(resultado.NotasRelease)
            ? "Esta versão foi publicada sem notas."
            : resultado.NotasRelease.Trim();
    }

    private void MostrarMensagem(string icone, string texto, bool erro = false)
    {
        PainelVersoes.Visibility = Visibility.Collapsed;
        PainelNotas.Visibility = Visibility.Collapsed;
        BtnAtualizar.Visibility = Visibility.Collapsed;
        PainelMensagem.Visibility = Visibility.Visible;

        TxtIconeMensagem.Text = icone;
        TxtMensagem.Text = texto;
        TxtIconeMensagem.Foreground = erro
            ? (System.Windows.Media.Brush)FindResource("LaranjaAvisoBrush")
            : (System.Windows.Media.Brush)FindResource("CinzaTextoBrush");
    }

    private async void BtnAtualizar_Click(object sender, RoutedEventArgs e)
    {
        if (_atualizando || _verificacao is null)
            return;

        // O aviso do banco não é excesso de zelo: o sistema aplica as migrations do
        // PostgreSQL ao abrir, e o banco é compartilhado por todos os PDVs. Atualizar um
        // caixa altera o esquema para os outros, que continuam rodando a versão antiga.
        var confirmacao = MessageBox.Show(
            $"O sistema será fechado, atualizado para a {_verificacao.VersaoPublicadaTexto} e reaberto automaticamente.\n\n" +
            "Antes de continuar:\n" +
            "• Finalize ou cancele qualquer venda em andamento neste caixa.\n" +
            "• Se houver outros caixas ligados neste mesmo banco, atualize todos — esta versão " +
            "pode alterar a estrutura do banco de dados ao abrir.\n\n" +
            "Suas configurações (.env) e as vendas gravadas em contingência são preservadas.",
            "Confirmar atualização",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirmacao != MessageBoxResult.OK)
            return;

        _atualizando = true;
        BtnAtualizar.IsEnabled = false;
        BtnCancelar.IsEnabled = false;
        BtnFechar.IsEnabled = false;
        PainelProgresso.Visibility = Visibility.Visible;

        var progresso = new Progress<ProgressoAtualizacaoDto>(p =>
        {
            TxtEtapa.Text = p.Etapa;
            if (p.Percentual is double percentual)
            {
                BarraProgresso.IsIndeterminate = false;
                BarraProgresso.Value = percentual;
            }
            else
            {
                BarraProgresso.IsIndeterminate = true;
            }
        });

        try
        {
            await _atualizador.BaixarEPrepararAsync(_verificacao, progresso);
        }
        catch (Exception ex)
        {
            // Nada foi trocado ainda — a troca só começa depois que este processo fecha, e ele
            // não vai fechar. Dá para continuar trabalhando normalmente na versão atual.
            PainelProgresso.Visibility = Visibility.Collapsed;
            MostrarMensagem("⚠",
                $"A atualização não pôde ser baixada e nada foi alterado no sistema.\n\n{ex.Message}",
                erro: true);

            _atualizando = false;
            BtnCancelar.IsEnabled = true;
            BtnFechar.IsEnabled = true;
            return;
        }

        TxtEtapa.Text = "Pronto. Fechando o sistema para aplicar...";
        BarraProgresso.Value = 100;

        MessageBox.Show(
            "Download concluído.\n\nO sistema será fechado agora e reabrirá sozinho em alguns segundos, já atualizado.",
            "Atualização",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        // O processo auxiliar já está esperando este PID sair para trocar os arquivos.
        System.Windows.Application.Current.Shutdown();
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
    {
        if (_atualizando)
            return;

        DialogResult = false;
        Close();
    }
}
