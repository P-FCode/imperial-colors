using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
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
    private static readonly Brush FundoSucesso = new SolidColorBrush(Color.FromRgb(212, 237, 218));
    private static readonly Brush TextoSucesso = new SolidColorBrush(Color.FromRgb(21, 87, 36));
    private static readonly Brush FundoErro = new SolidColorBrush(Color.FromRgb(248, 215, 218));
    private static readonly Brush TextoErro = new SolidColorBrush(Color.FromRgb(114, 28, 36));

    private readonly IServiceProvider _serviceProvider;
    private readonly IAppConfigService _config;
    private readonly IConfiguracoesAplicacaoService _configuracoes;
    private readonly ISessaoService _sessaoService;
    private readonly IAtualizadorSistemaService _atualizador;

    private Button? _cardAtivo;
    private bool _suprimirMascaraCnpj;

    public ConfiguracoesView(IServiceProvider serviceProvider, ISessaoService sessaoService)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
        _sessaoService = sessaoService;
        _config = serviceProvider.GetRequiredService<IAppConfigService>();
        _configuracoes = serviceProvider.GetRequiredService<IConfiguracoesAplicacaoService>();
        _atualizador = serviceProvider.GetRequiredService<IAtualizadorSistemaService>();

        PainelPerifericos.Content = serviceProvider.GetRequiredService<PerifericosView>();
        PainelAuditoria.Content = serviceProvider.GetRequiredService<AuditoriaLogsView>();
        PainelFiscal.Content = serviceProvider.GetRequiredService<FiscalConfigView>();
        PainelNaturezasOperacao.Content = serviceProvider.GetRequiredService<NaturezaOperacaoView>();

        if (_sessaoService.EhAdmin)
        {
            BtnCardUsuarios.Visibility = Visibility.Visible;
            GridSubmodulos.Columns = 6;
            PainelGestaoUsuarios.Content = serviceProvider.GetRequiredService<GestaoUsuariosView>();
        }
        else
        {
            BtnCardUsuarios.Visibility = Visibility.Collapsed;
            GridSubmodulos.Columns = 5;
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
        PainelAuditoria.Visibility = Visibility.Collapsed;
        PainelFiscal.Visibility = Visibility.Collapsed;
        PainelNaturezasOperacao.Visibility = Visibility.Collapsed;

        painel.Visibility = Visibility.Visible;
    }

    private void BtnCardGeral_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardGeral, PainelGeral);

    private void BtnCardPerifericos_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardPerifericos, PainelPerifericos);

    private void BtnCardUsuarios_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardUsuarios, PainelGestaoUsuarios);

    private void BtnCardAuditoria_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardAuditoria, PainelAuditoria);

    private void BtnCardFiscal_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardFiscal, PainelFiscal);

    private void BtnCardNaturezasOperacao_Click(object sender, RoutedEventArgs e)
        => SelecionarSubmodulo(BtnCardNaturezasOperacao, PainelNaturezasOperacao);

    private void CarregarConfiguracoes()
    {
        TxtInfoEnv.Text = $"Configurações gravadas em {_configuracoes.CaminhoArquivoEnv}. " +
                          "Empresa e preferências podem ser editadas por aqui mesmo; " +
                          "a conexão com o banco é definida na instalação e só muda pelo arquivo.";

        CarregarBanco();
        CarregarEmpresa();
        CarregarGerais();

        TxtSobreEmpresa.Text = $"{_config.EmpresaNome} - Sistema de Gestão";

        // Lida do assembly, nao escrita a mao: o workflow de release grava a versao a partir
        // da tag do GitHub, e e essa mesma versao que o atualizador compara. Um numero fixo
        // aqui divergiria da instalacao real na primeira atualizacao automatica.
        TxtVersaoSistema.Text = $"Versão {_atualizador.VersaoInstaladaTexto}";
    }

    /// <summary>
    /// A conexão continua sendo definida só pelo .env: trocar servidor/banco em produção é
    /// operação de instalação, não de operador de caixa. Aqui ela é apenas exibida (com a
    /// senha mascarada) e pode ser testada.
    /// </summary>
    private void CarregarBanco()
        => TxtConnectionString.Text = MascararSenha(_config.ConnectionString);

    private static string MascararSenha(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return string.Empty;

        var partes = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < partes.Length; i++)
        {
            if (partes[i].TrimStart().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
                partes[i] = "Password=********";
        }

        return string.Join(';', partes) + ";";
    }

    private void CarregarEmpresa()
    {
        var empresa = _configuracoes.ObterEmpresaAtual();

        TxtEmpresaNome.Text = empresa.NomeFantasia;
        TxtEmpresaRazaoSocial.Text = empresa.RazaoSocial;
        TxtEmpresaSubtitulo.Text = empresa.Subtitulo;
        TxtEmpresaIe.Text = empresa.InscricaoEstadual;
        TxtEmpresaTelefone.Text = empresa.Telefone;
        TxtEmpresaEmail.Text = empresa.Email;
        TxtEmpresaEndereco.Text = empresa.Endereco;

        _suprimirMascaraCnpj = true;
        TxtEmpresaCnpj.Text = DocumentoHelper.AplicarMascaraCnpj(empresa.Cnpj);
        _suprimirMascaraCnpj = false;
    }

    private void CarregarGerais()
    {
        var gerais = _configuracoes.ObterGeraisAtual();

        TxtCupomRodape.Text = gerais.CupomRodape;
        TxtPastaBackup.Text = gerais.PastaBackup;
    }

    private ConfiguracoesEmpresaInput MontarInputEmpresa() => new(
        TxtEmpresaNome.Text,
        TxtEmpresaRazaoSocial.Text,
        TxtEmpresaSubtitulo.Text,
        TxtEmpresaCnpj.Text,
        TxtEmpresaIe.Text,
        TxtEmpresaEndereco.Text,
        TxtEmpresaTelefone.Text,
        TxtEmpresaEmail.Text);

    private ConfiguracoesGeraisInput MontarInputGerais() => new(
        TxtCupomRodape.Text,
        TxtPastaBackup.Text);

    /// <summary>Testa a conexão que o sistema está realmente usando, a do .env.</summary>
    private async void BtnTestarConexao_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button botao)
            return;

        botao.IsEnabled = false;
        ExibirStatus(StatusConexao, TxtStatusConexao, "Testando conexão...", sucesso: true);

        try
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var contexto = await contextFactory.CreateDbContextAsync();

            if (await contexto.Database.CanConnectAsync())
                ExibirStatus(StatusConexao, TxtStatusConexao, "✓ Conexão estabelecida com sucesso!", sucesso: true);
            else
                ExibirStatus(StatusConexao, TxtStatusConexao,
                    "✗ Não foi possível conectar ao banco de dados.", sucesso: false);
        }
        catch (Exception ex)
        {
            ExibirStatus(StatusConexao, TxtStatusConexao,
                $"✗ {ExceptionMessageHelper.ObterMensagemAmigavel(ex)}", sucesso: false);
        }
        finally
        {
            botao.IsEnabled = true;
        }
    }

    private async void BtnSalvarEmpresa_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvarEmpresa.IsEnabled = false;

        try
        {
            await _configuracoes.SalvarEmpresaAsync(MontarInputEmpresa());

            CarregarEmpresa();
            TxtSobreEmpresa.Text = $"{_config.EmpresaNome} - Sistema de Gestão";

            ExibirStatus(StatusEmpresa, TxtStatusEmpresa,
                "✓ Dados da empresa salvos. Já valem para cupons, relatórios e orçamentos.", sucesso: true);
        }
        catch (DomainException ex)
        {
            ExibirStatus(StatusEmpresa, TxtStatusEmpresa, $"✗ {ex.Message}", sucesso: false);
        }
        catch (Exception ex)
        {
            ExibirStatus(StatusEmpresa, TxtStatusEmpresa,
                $"✗ {ExceptionMessageHelper.ObterMensagemAmigavel(ex)}", sucesso: false);
        }
        finally
        {
            BtnSalvarEmpresa.IsEnabled = true;
        }
    }

    private void BtnDesfazerEmpresa_Click(object sender, RoutedEventArgs e)
    {
        CarregarEmpresa();
        StatusEmpresa.Visibility = Visibility.Collapsed;
    }

    private async void BtnSalvarGerais_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvarGerais.IsEnabled = false;

        try
        {
            await _configuracoes.SalvarGeraisAsync(MontarInputGerais());
            CarregarGerais();

            ExibirStatus(StatusGerais, TxtStatusGerais, "✓ Preferências salvas.", sucesso: true);
        }
        catch (DomainException ex)
        {
            ExibirStatus(StatusGerais, TxtStatusGerais, $"✗ {ex.Message}", sucesso: false);
        }
        catch (Exception ex)
        {
            ExibirStatus(StatusGerais, TxtStatusGerais,
                $"✗ {ExceptionMessageHelper.ObterMensagemAmigavel(ex)}", sucesso: false);
        }
        finally
        {
            BtnSalvarGerais.IsEnabled = true;
        }
    }

    private void TxtEmpresaCnpj_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirMascaraCnpj)
            return;

        _suprimirMascaraCnpj = true;

        var cursor = Math.Min(TxtEmpresaCnpj.SelectionStart, TxtEmpresaCnpj.Text.Length);
        var digitosAntes = DocumentoHelper.SomenteDigitos(TxtEmpresaCnpj.Text[..cursor]).Length;

        TxtEmpresaCnpj.Text = DocumentoHelper.AplicarMascaraCnpj(TxtEmpresaCnpj.Text);
        TxtEmpresaCnpj.SelectionStart = Math.Min(TxtEmpresaCnpj.Text.Length, digitosAntes + SeparadoresCnpjAte(digitosAntes));

        _suprimirMascaraCnpj = false;
    }

    /// <summary>Pontuação já digitada até o N-ésimo dígito da máscara XX.XXX.XXX/XXXX-XX.</summary>
    private static int SeparadoresCnpjAte(int digitos)
        => (digitos > 2 ? 1 : 0) + (digitos > 5 ? 1 : 0) + (digitos > 8 ? 1 : 0) + (digitos > 12 ? 1 : 0);

    private static void ExibirStatus(Border container, TextBlock texto, string mensagem, bool sucesso)
    {
        container.Visibility = Visibility.Visible;
        container.Background = sucesso ? FundoSucesso : FundoErro;
        texto.Text = mensagem;
        texto.Foreground = sucesso ? TextoSucesso : TextoErro;
    }

    private void BtnAtualizarSistema_Click(object sender, RoutedEventArgs e)
    {
        BtnAtualizarSistema.IsEnabled = false;
        TxtStatusAtualizacao.Text = string.Empty;

        try
        {
            var dialogo = _serviceProvider.GetRequiredService<AtualizacaoSistemaDialogView>();
            dialogo.Owner = Window.GetWindow(this);
            dialogo.ShowDialog();
        }
        catch (Exception ex)
        {
            TxtStatusAtualizacao.Text = ExceptionMessageHelper.ObterMensagemAmigavel(ex);
        }
        finally
        {
            BtnAtualizarSistema.IsEnabled = true;
        }
    }

    private void BtnAbrirEnv_Click(object sender, RoutedEventArgs e)
    {
        var caminhoEnv = _configuracoes.CaminhoArquivoEnv;

        if (!File.Exists(caminhoEnv))
        {
            MessageBox.Show("Arquivo .env não encontrado. Copie o .env.example para .env.",
                "Arquivo não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = caminhoEnv,
            UseShellExecute = true
        });
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
