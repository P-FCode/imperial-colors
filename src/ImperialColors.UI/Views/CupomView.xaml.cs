using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using System.Windows;

namespace ImperialColors.UI.Views;

public partial class CupomView : Window
{
    private readonly IRelatorioService _relatorioService;
    private readonly IAppConfigService _config;
    private readonly ILocalConfigService _localConfig;
    private VendaDto? _venda;

    public CupomView(IRelatorioService relatorioService, IAppConfigService config, ILocalConfigService localConfig)
    {
        InitializeComponent();
        _relatorioService = relatorioService;
        _config = config;
        _localConfig = localConfig;
        LogoHelper.AplicarIconeJanela(this, _config.IconPath);
    }

    public void InicializarVenda(VendaDto venda)
    {
        _venda = venda;

        var empresa = _config.Empresa;

        TxtEmpresaNome.Text = empresa.NomeFantasia;
        TxtRodape.Text = _config.CupomRodape;

        if (!string.IsNullOrWhiteSpace(empresa.RazaoSocial))
            TxtEmpresaRazaoSocial.Text = empresa.RazaoSocial;
        else
            TxtEmpresaRazaoSocial.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(empresa.CNPJ))
            TxtEmpresaCnpj.Text = $"CNPJ: {empresa.CNPJ}";
        else
            TxtEmpresaCnpj.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(empresa.InscricaoEstadual))
            TxtEmpresaIe.Text = $"IE: {empresa.InscricaoEstadual}";
        else
            TxtEmpresaIe.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(empresa.Endereco))
            TxtEmpresaEndereco.Text = empresa.Endereco;
        else
            TxtEmpresaEndereco.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrWhiteSpace(empresa.Telefone))
            TxtEmpresaTelefone.Text = $"Tel: {empresa.Telefone}";
        else
            TxtEmpresaTelefone.Visibility = Visibility.Collapsed;

        TxtNumeroVenda.Text = venda.NumeroVenda;
        TxtData.Text = FormattingHelper.FormatarDataHora(venda.DataVenda);
        TxtComprador.Text = MontarTextoComprador(venda);
        PainelComprador.Visibility = Visibility.Visible;

        TxtSubtotal.Text = FormattingHelper.FormatarMoeda(venda.Subtotal);
        TxtDesconto.Text = FormattingHelper.FormatarMoeda(venda.Desconto);
        var totalItens = venda.Itens.Sum(i => i.Quantidade);
        TxtTotalItens.Text = FormattingHelper.FormatarQuantidade(totalItens);
        TxtTotal.Text = FormattingHelper.FormatarMoeda(venda.Total);

        var pagamentosExibicao = venda.Pagamentos.Count > 0
            ? venda.Pagamentos.Select(p => new PagamentoCupomUi(p)).ToList()
            :
            [
                new PagamentoCupomUi(new VendaPagamentoDto
                {
                    FormaPagamento = venda.FormaPagamento,
                    Valor = venda.Total,
                    ValorRecebido = venda.ValorPago,
                    QuantidadeParcelas = venda.QuantidadeParcelas
                })
            ];

        LstPagamentos.ItemsSource = pagamentosExibicao;

        if (venda.Troco > 0)
        {
            PainelTrocoCupom.Visibility = Visibility.Visible;
            TxtTroco.Text = FormattingHelper.FormatarMoeda(venda.Troco);
        }
        else
        {
            PainelTrocoCupom.Visibility = Visibility.Collapsed;
        }

        LstItens.ItemsSource = venda.Itens;
        AjustarAlturaJanela();
    }

    private static string MontarTextoComprador(VendaDto venda)
    {
        var nome = venda.NomeCompradorExibicao;
        if (string.IsNullOrWhiteSpace(venda.DocumentoCompradorExibicao))
            return nome;

        var rotulo = venda.TipoPessoaComprador == Domain.Enums.TipoPessoa.Juridica ? "CNPJ" : "CPF";
        return $"{nome} — {rotulo}: {venda.DocumentoCompradorExibicao}";
    }

    private void AjustarAlturaJanela()
    {
        MaxHeight = SystemParameters.WorkArea.Height * 0.92;
        ScrollCupom.MaxHeight = MaxHeight - 90;
        UpdateLayout();
    }

    private void BtnImprimir_Click(object sender, RoutedEventArgs e)
    {
        if (!CupomPrintHelper.ImprimirNaImpressoraConfigurada(
                BorderCupom,
                $"Cupom {_venda?.NumeroVenda}",
                _localConfig.ImpressoraSelecionada,
                out var erro))
        {
            MessageBox.Show(erro ?? "Não foi possível imprimir o cupom.",
                "Impressão", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BtnSalvarPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_venda is null) return;

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Cupom_{_venda.NumeroVenda}",
            DefaultExt = ".pdf",
            Filter = "PDF|*.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                await _relatorioService.GerarCupomPdfAsync(_venda, saveDialog.FileName);
                MessageBox.Show($"PDF salvo em:\n{saveDialog.FileName}", "PDF Gerado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar PDF: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();

    private sealed class PagamentoCupomUi(VendaPagamentoDto pagamento)
    {
        public string Descricao => pagamento.Descricao;
        public string ValorFormatado => FormattingHelper.FormatarMoeda(pagamento.Valor);
    }
}
