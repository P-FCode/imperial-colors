using ImperialColors.Application.Interfaces;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ImperialColors.UI.Views;

public partial class PerifericosView : UserControl
{
    private readonly IPrinterService _printerService;
    private readonly ILocalConfigService _localConfig;
    private readonly DispatcherTimer _timerLimparScanner;

    public PerifericosView(IPrinterService printerService, ILocalConfigService localConfig)
    {
        InitializeComponent();
        _printerService = printerService;
        _localConfig = localConfig;

        _timerLimparScanner = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timerLimparScanner.Tick += (_, _) =>
        {
            _timerLimparScanner.Stop();
            TxtTesteScanner.Clear();
        };

        CarregarImpressoras();
    }

    private void CarregarImpressoras()
    {
        var impressoras = _printerService.ListarImpressoras();
        CmbImpressoras.ItemsSource = impressoras;

        if (!impressoras.Any())
        {
            PainelAvisoImpressora.Visibility = Visibility.Visible;
            TxtAvisoImpressora.Text =
                "Nenhuma impressora instalada foi detectada neste computador. Instale uma impressora no Windows e clique em Atualizar Lista.";
            return;
        }

        PainelAvisoImpressora.Visibility = Visibility.Collapsed;

        var selecionada = _localConfig.ImpressoraSelecionada;
        if (!string.IsNullOrWhiteSpace(selecionada) && impressoras.Contains(selecionada))
            CmbImpressoras.SelectedItem = selecionada;
        else if (!string.IsNullOrWhiteSpace(selecionada))
        {
            PainelAvisoImpressora.Visibility = Visibility.Visible;
            TxtAvisoImpressora.Text =
                $"A impressora salva '{selecionada}' não foi encontrada. Selecione outra e salve novamente.";
            CmbImpressoras.SelectedIndex = 0;
        }
        else
        {
            var padrao = _printerService.ObterImpressoraPadraoSistema();
            CmbImpressoras.SelectedItem = padrao ?? impressoras.FirstOrDefault();
        }
    }

    private async void BtnSalvarImpressora_Click(object sender, RoutedEventArgs e)
    {
        if (CmbImpressoras.SelectedItem is not string impressora)
        {
            MessageBox.Show("Selecione uma impressora.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _localConfig.ImpressoraSelecionada = impressora;
        await _localConfig.SalvarAsync();
        PainelAvisoImpressora.Visibility = Visibility.Visible;
        PainelAvisoImpressora.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
        TxtAvisoImpressora.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
        TxtAvisoImpressora.Text = $"Impressora '{impressora}' salva com sucesso.";
    }

    private void BtnAtualizarImpressoras_Click(object sender, RoutedEventArgs e) => CarregarImpressoras();

    private void TxtTesteScanner_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        var codigo = TxtTesteScanner.Text.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        PainelSucessoScanner.Visibility = Visibility.Visible;
        TxtSucessoScanner.Text = $"Leitor configurado com sucesso! Código lido: {codigo}";

        _timerLimparScanner.Stop();
        _timerLimparScanner.Start();
    }
}
