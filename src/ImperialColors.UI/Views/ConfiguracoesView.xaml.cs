using ImperialColors.Infrastructure.Data;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.IO;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Media;



namespace ImperialColors.UI.Views;



public partial class ConfiguracoesView : UserControl

{

    private readonly IServiceProvider _serviceProvider;

    private readonly IAppConfigService _config;

    private readonly ISessaoService _sessaoService;

    private Button? _cardAtivo;



    public ConfiguracoesView(IServiceProvider serviceProvider, ISessaoService sessaoService)

    {

        InitializeComponent();

        _serviceProvider = serviceProvider;

        _sessaoService = sessaoService;

        _config = serviceProvider.GetRequiredService<IAppConfigService>();



        PainelPerifericos.Content = serviceProvider.GetRequiredService<PerifericosView>();



        if (_sessaoService.EhAdmin)

        {

            BtnCardUsuarios.Visibility = Visibility.Visible;

            GridSubmodulos.Columns = 3;

            PainelGestaoUsuarios.Content = serviceProvider.GetRequiredService<GestaoUsuariosView>();

        }

        else

        {

            BtnCardUsuarios.Visibility = Visibility.Collapsed;

            GridSubmodulos.Columns = 2;

        }



        CarregarConfiguracoes();

        SelecionarSubmodulo(BtnCardGeral, PainelGeral);

    }



    private void SelecionarSubmodulo(Button card, UIElement painel)

    {

        if (_cardAtivo is not null)

            NavMenuHelper.SetIsActive(_cardAtivo, false);



        _cardAtivo = card;

        NavMenuHelper.SetIsActive(card, true);



        PainelGeral.Visibility = Visibility.Collapsed;

        PainelPerifericos.Visibility = Visibility.Collapsed;

        PainelGestaoUsuarios.Visibility = Visibility.Collapsed;



        painel.Visibility = Visibility.Visible;

    }



    private void BtnCardGeral_Click(object sender, RoutedEventArgs e)

        => SelecionarSubmodulo(BtnCardGeral, PainelGeral);



    private void BtnCardPerifericos_Click(object sender, RoutedEventArgs e)

        => SelecionarSubmodulo(BtnCardPerifericos, PainelPerifericos);



    private void BtnCardUsuarios_Click(object sender, RoutedEventArgs e)

        => SelecionarSubmodulo(BtnCardUsuarios, PainelGestaoUsuarios);



    private void CarregarConfiguracoes()

    {

        TxtInfoEnv.Text = "Dados da empresa em appsettings.json (DadosEmpresa) ou variáveis .env. Edite e reinicie o sistema para aplicar.";



        TxtConnectionString.Text = MascararSenha(_config.ConnectionString);

        TxtEmpresaNome.Text = _config.EmpresaNome;

        TxtEmpresaRazaoSocial.Text = _config.EmpresaRazaoSocial;

        TxtEmpresaSubtitulo.Text = _config.EmpresaSubtitulo;

        TxtEmpresaCnpj.Text = FormatarCnpj(_config.EmpresaCnpj);

        TxtEmpresaIe.Text = _config.Empresa.InscricaoEstadual;

        TxtEmpresaTelefone.Text = _config.EmpresaTelefone;

        TxtEmpresaEmail.Text = _config.EmpresaEmail;

        TxtEmpresaEndereco.Text = _config.EmpresaEndereco;

        TxtSobreEmpresa.Text = $"{_config.EmpresaNome} - Sistema de Gestão";

    }



    private static string FormatarCnpj(string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
            return cnpj;

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }



    private static string MascararSenha(string connectionString)

    {

        if (string.IsNullOrWhiteSpace(connectionString))

            return string.Empty;



        var partes = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < partes.Length; i++)

        {

            if (partes[i].TrimStart().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))

                partes[i] = "Password=********";

        }

        return string.Join(';', partes) + ";";

    }



    private async void BtnTestarConexao_Click(object sender, RoutedEventArgs e)

    {

        try

        {

            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var ctx = await contextFactory.CreateDbContextAsync();
            var pode = await ctx.Database.CanConnectAsync();

            StatusConexao.Visibility = Visibility.Visible;

            if (pode)

            {

                StatusConexao.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));

                TxtStatusConexao.Text = "✓ Conexão com o banco de dados estabelecida com sucesso!";

                TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));

            }

            else

            {

                StatusConexao.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));

                TxtStatusConexao.Text = "✗ Não foi possível conectar ao banco de dados.";

                TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));

            }

        }

        catch (Exception ex)

        {

            StatusConexao.Visibility = Visibility.Visible;

            StatusConexao.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));

            TxtStatusConexao.Text = $"✗ Erro: {ex.Message}";

            TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));

        }

    }



    private void BtnAbrirEnv_Click(object sender, RoutedEventArgs e)

    {

        var caminhoEnv = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

        if (!File.Exists(caminhoEnv))

            caminhoEnv = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".env"));



        if (File.Exists(caminhoEnv))

        {

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo

            {

                FileName = caminhoEnv,

                UseShellExecute = true

            });

        }

        else

        {

            MessageBox.Show("Arquivo .env não encontrado. Copie o .env.example para .env.",

                "Arquivo não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);

        }

    }

    private async void BtnGerarManual_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Manual_Imperial_Colors_{DateTime.Now:yyyyMMdd}",
            DefaultExt = ".pdf",
            Filter = "PDF|*.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var svc = _serviceProvider.GetRequiredService<DocumentosPdfService>();
            await svc.GerarManualPdfAsync(dialog.FileName);

            MessageBox.Show($"Manual gerado com sucesso:\n{dialog.FileName}",
                "Documento gerado", MessageBoxButton.OK, MessageBoxImage.Information);

            // O arquivo foi 100% gravado e liberado antes deste ponto
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = dialog.FileName,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar Manual: {ex.Message}",
                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}


