using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class FiscalConfigView : UserControl
{
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;
    private readonly IViaCepService _viaCepService;
    private readonly ObservableCollection<InscricaoEstadualItem> _inscricoesSubstituto = new();

    public FiscalConfigView(IConfiguracaoFiscalService configuracaoFiscal, IViaCepService viaCepService)
    {
        InitializeComponent();
        _configuracaoFiscal = configuracaoFiscal;
        _viaCepService = viaCepService;
        GridInscricoesSubstituto.ItemsSource = _inscricoesSubstituto;

        _ = CarregarConfiguracaoAsync();
    }

    private async Task CarregarConfiguracaoAsync()
    {
        RegimeTributario regime;
        ConfiguracaoFiscalEmpresaDto empresa;
        try
        {
            regime = await _configuracaoFiscal.ObterRegimeAsync();
            empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync();
        }
        catch
        {
            // Falha ao carregar configuração fiscal não deve travar a tela — o admin
            // ainda consegue preencher do zero e salvar.
            return;
        }

        SelecionarItemPorTag(CmbRegime, regime.ToString());
        ChkSimplesExcessoSublimite.IsChecked = empresa.SimplesExcessoSublimite;

        ChkIeIsenta.IsChecked = empresa.IeIsenta;
        TxtInscricaoMunicipal.Text = empresa.InscricaoMunicipal ?? string.Empty;
        TxtInscricaoSuframa.Text = empresa.InscricaoSuframa ?? string.Empty;
        TxtCnae.Text = empresa.Cnae ?? string.Empty;

        _inscricoesSubstituto.Clear();
        foreach (var inscricao in empresa.InscricoesSubstitutoTributario)
            _inscricoesSubstituto.Add(new InscricaoEstadualItem { Uf = inscricao.Uf, InscricaoEstadual = inscricao.InscricaoEstadual });

        ChkDifalNaoContribuinte.IsChecked = empresa.DifalNaoContribuinte;
        ChkDifalStContribuinte.IsChecked = empresa.DifalStContribuinte;

        TxtCep.Text = empresa.Cep ?? string.Empty;
        TxtLogradouro.Text = empresa.Logradouro ?? string.Empty;
        TxtNumero.Text = empresa.Numero ?? string.Empty;
        TxtComplemento.Text = empresa.Complemento ?? string.Empty;
        TxtBairro.Text = empresa.Bairro ?? string.Empty;
        TxtCodigoIbge.Text = empresa.CodigoMunicipioIbge ?? string.Empty;
        TxtNomeMunicipio.Text = empresa.NomeMunicipio ?? string.Empty;
        TxtUf.Text = empresa.Uf ?? string.Empty;

        TxtSerie.Text = string.IsNullOrWhiteSpace(empresa.Serie) ? "1" : empresa.Serie;
        SelecionarItemPorTag(CmbAmbiente, empresa.Ambiente.ToString());

        TxtIdCscHomologacao.Text = empresa.IdCscHomologacao ?? string.Empty;
        TxtCscHomologacao.Password = empresa.CscHomologacao ?? string.Empty;
        TxtIdCscProducao.Text = empresa.IdCscProducao ?? string.Empty;
        TxtCscProducao.Password = empresa.CscProducao ?? string.Empty;

        TxtCstIbsCbsPadrao.Text = empresa.CstIbsCbsPadrao ?? string.Empty;
        TxtCClassTribPadrao.Text = empresa.CClassTribPadrao ?? string.Empty;

        TxtAliquotaIbsUf.Text = empresa.AliquotaIbsUfPadrao.HasValue
            ? FormatarPercentual(empresa.AliquotaIbsUfPadrao.Value) : "0,10";
        TxtAliquotaIbsMunicipio.Text = empresa.AliquotaIbsMunicipioPadrao.HasValue
            ? FormatarPercentual(empresa.AliquotaIbsMunicipioPadrao.Value) : "0,00";
        TxtAliquotaCbs.Text = empresa.AliquotaCbsPadrao.HasValue
            ? FormatarPercentual(empresa.AliquotaCbsPadrao.Value) : "0,90";

        ChkValidarNcmEmNotas.IsChecked = empresa.ValidarNcmEmNotas;
        ChkBloquearEdicaoNumeroNota.IsChecked = empresa.BloquearEdicaoNumeroNota;
        ChkBloquearNotaItensMenorVenda.IsChecked = empresa.BloquearNotaComItensMenorQueVenda;
        ChkGerarNotaAutomatica.IsChecked = empresa.GerarNotaAutomaticaAoFinalizarVenda;
        ChkCancelarNotaAutomatico.IsChecked = empresa.CancelarNotaAutomaticoAoCancelarVenda;
        SelecionarItemPorTag(CmbFretePorConta, empresa.FretePorContaPadrao.ToString());
        SelecionarItemPorTag(CmbIndicadorPresenca, empresa.IndicadorPresencaPadrao.ToString());
        TxtEmailPadraoEnvioNotas.Text = empresa.EmailPadraoEnvioNotas ?? string.Empty;
    }

    private async void BtnBuscarCep_Click(object sender, RoutedEventArgs e)
    {
        var cep = TxtCep.Text?.Trim();
        if (string.IsNullOrWhiteSpace(cep))
        {
            MessageBox.Show("Digite o CEP antes de buscar.", "Busca de CEP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        EnderecoViaCepDto? endereco;
        try
        {
            endereco = await _viaCepService.ConsultarAsync(cep);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Busca de CEP",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (endereco is null)
        {
            MessageBox.Show("CEP não encontrado.", "Busca de CEP", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtLogradouro.Text = endereco.Logradouro;
        TxtBairro.Text = endereco.Bairro;
        TxtNomeMunicipio.Text = endereco.Cidade;
        TxtUf.Text = endereco.Uf;
        if (!string.IsNullOrWhiteSpace(endereco.Complemento))
            TxtComplemento.Text = endereco.Complemento;

        if (!string.IsNullOrWhiteSpace(endereco.CodigoIbge))
        {
            TxtCodigoIbge.Text = endereco.CodigoIbge;
        }
        else
        {
            TxtCodigoIbge.Text = string.Empty;
            MessageBox.Show(
                "CEP encontrado, mas a resposta não trouxe o código IBGE do município. Preencha manualmente ou tente novamente — a NF-e exige esse campo.",
                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CmbAmbiente_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AvisoProducao is null)
            return;

        var tag = (CmbAmbiente.SelectedItem as ComboBoxItem)?.Tag as string;
        AvisoProducao.Visibility = tag == nameof(AmbienteEmissaoFiscal.Producao)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void BtnAdicionarInscricaoSubstituto_Click(object sender, RoutedEventArgs e)
        => _inscricoesSubstituto.Add(new InscricaoEstadualItem());

    private void BtnRemoverInscricaoSubstituto_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: InscricaoEstadualItem item })
            _inscricoesSubstituto.Remove(item);
    }

    private async void BtnSalvarConfiguracaoFiscal_Click(object sender, RoutedEventArgs e)
    {
        if (CmbRegime.SelectedItem is not ComboBoxItem regimeItem || regimeItem.Tag is not string regimeTag ||
            !Enum.TryParse<RegimeTributario>(regimeTag, out var regime))
        {
            MessageBox.Show("Selecione um regime tributário.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (CmbAmbiente.SelectedItem is not ComboBoxItem ambienteItem || ambienteItem.Tag is not string ambienteTag ||
            !Enum.TryParse<AmbienteEmissaoFiscal>(ambienteTag, out var ambiente))
        {
            MessageBox.Show("Selecione o ambiente de emissão.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ambiente == AmbienteEmissaoFiscal.Producao)
        {
            var confirmar = MessageBox.Show(
                "Você está definindo o ambiente de emissão como PRODUÇÃO. Quando a emissão de NF-e/NFC-e for implementada, notas emitidas nesse ambiente têm valor fiscal real. Confirma?",
                "Confirmar ambiente de Produção", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmar != MessageBoxResult.Yes)
                return;
        }

        var fretePorConta = ObterTagSelecionada<ModalidadeFrete>(CmbFretePorConta) ?? ModalidadeFrete.SemOcorrenciaTransporte;
        var indicadorPresenca = ObterTagSelecionada<IndicadorPresencaComprador>(CmbIndicadorPresenca) ?? IndicadorPresencaComprador.Presencial;

        FormattingHelper.TryParseMoedaOpcional(TxtAliquotaIbsUf.Text, out var aliquotaIbsUf);
        FormattingHelper.TryParseMoedaOpcional(TxtAliquotaIbsMunicipio.Text, out var aliquotaIbsMunicipio);
        FormattingHelper.TryParseMoedaOpcional(TxtAliquotaCbs.Text, out var aliquotaCbs);

        var dto = new ConfiguracaoFiscalEmpresaDto
        {
            IeIsenta = ChkIeIsenta.IsChecked == true,
            InscricaoMunicipal = TextoOuNulo(TxtInscricaoMunicipal.Text),
            InscricaoSuframa = TextoOuNulo(TxtInscricaoSuframa.Text),
            Cnae = NormalizarSomenteDigitos(TxtCnae.Text),
            DifalNaoContribuinte = ChkDifalNaoContribuinte.IsChecked == true,
            DifalStContribuinte = ChkDifalStContribuinte.IsChecked == true,
            Cep = NormalizarSomenteDigitos(TxtCep.Text),
            Logradouro = TextoOuNulo(TxtLogradouro.Text),
            Numero = TextoOuNulo(TxtNumero.Text),
            Complemento = TextoOuNulo(TxtComplemento.Text),
            Bairro = TextoOuNulo(TxtBairro.Text),
            CodigoMunicipioIbge = NormalizarSomenteDigitos(TxtCodigoIbge.Text),
            NomeMunicipio = TextoOuNulo(TxtNomeMunicipio.Text),
            Uf = TextoOuNulo(TxtUf.Text),
            Serie = NormalizarSomenteDigitos(TxtSerie.Text),
            Ambiente = ambiente,
            IdCscHomologacao = TextoOuNulo(TxtIdCscHomologacao.Text),
            CscHomologacao = TextoOuNulo(TxtCscHomologacao.Password),
            IdCscProducao = TextoOuNulo(TxtIdCscProducao.Text),
            CscProducao = TextoOuNulo(TxtCscProducao.Password),
            SimplesExcessoSublimite = ChkSimplesExcessoSublimite.IsChecked == true,
            AliquotaIbsUfPadrao = aliquotaIbsUf,
            AliquotaIbsMunicipioPadrao = aliquotaIbsMunicipio,
            AliquotaCbsPadrao = aliquotaCbs,
            CstIbsCbsPadrao = NormalizarSomenteDigitos(TxtCstIbsCbsPadrao.Text),
            CClassTribPadrao = NormalizarSomenteDigitos(TxtCClassTribPadrao.Text),
            ValidarNcmEmNotas = ChkValidarNcmEmNotas.IsChecked == true,
            BloquearEdicaoNumeroNota = ChkBloquearEdicaoNumeroNota.IsChecked == true,
            BloquearNotaComItensMenorQueVenda = ChkBloquearNotaItensMenorVenda.IsChecked == true,
            FretePorContaPadrao = fretePorConta,
            EmailPadraoEnvioNotas = TextoOuNulo(TxtEmailPadraoEnvioNotas.Text),
            IndicadorPresencaPadrao = indicadorPresenca,
            GerarNotaAutomaticaAoFinalizarVenda = ChkGerarNotaAutomatica.IsChecked == true,
            CancelarNotaAutomaticoAoCancelarVenda = ChkCancelarNotaAutomatico.IsChecked == true,
            InscricoesSubstitutoTributario = _inscricoesSubstituto
                .Where(i => !string.IsNullOrWhiteSpace(i.Uf) || !string.IsNullOrWhiteSpace(i.InscricaoEstadual))
                .Select(i => new InscricaoEstadualSubstitutoDto { Uf = i.Uf.Trim(), InscricaoEstadual = i.InscricaoEstadual.Trim() })
                .ToList()
        };

        BtnSalvarConfiguracaoFiscal.IsEnabled = false;
        try
        {
            await _configuracaoFiscal.DefinirRegimeAsync(regime);
            await _configuracaoFiscal.SalvarConfiguracaoEmpresaAsync(dto);

            ExibirStatus("Configuração fiscal salva.", sucesso: true);
        }
        catch (DomainException ex)
        {
            ExibirStatus(ex.Message, sucesso: false);
        }
        catch (Exception ex)
        {
            ExibirStatus(ExceptionMessageHelper.ObterMensagemAmigavel(ex), sucesso: false);
        }
        finally
        {
            BtnSalvarConfiguracaoFiscal.IsEnabled = true;
        }
    }

    private void ExibirStatus(string mensagem, bool sucesso)
    {
        TxtStatusConfiguracao.Text = mensagem;
        TxtStatusConfiguracao.Foreground = (System.Windows.Media.Brush)FindResource(
            sucesso ? "VerdeSucessoBrush" : "VermelhoErroBrush");
        TxtStatusConfiguracao.Visibility = Visibility.Visible;
    }

    private static void SelecionarItemPorTag(ComboBox combo, string tag)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag as string == tag)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private static TEnum? ObterTagSelecionada<TEnum>(ComboBox combo) where TEnum : struct, Enum
        => combo.SelectedItem is ComboBoxItem item && item.Tag is string tag && Enum.TryParse<TEnum>(tag, out var valor)
            ? valor
            : null;

    private static string FormatarPercentual(decimal valor)
        => valor.ToString("0.####", FormattingHelper.CulturaPtBr);

    private static string? TextoOuNulo(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static string? NormalizarSomenteDigitos(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;

        var digitos = new string(texto.Where(char.IsDigit).ToArray());
        return digitos.Length == 0 ? null : digitos;
    }

    /// <summary>Linha editável da grade de Inscrições Estaduais de Substituto Tributário.</summary>
    private class InscricaoEstadualItem
    {
        public string Uf { get; set; } = string.Empty;
        public string InscricaoEstadual { get; set; } = string.Empty;
    }
}
