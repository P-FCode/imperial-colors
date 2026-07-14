using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImperialColors.UI.Views;

public partial class TrocaFormView : Window
{
    private readonly ITrocaService _trocaService;
    private readonly IProdutoService _produtoService;

    private bool _modoVendaExterna;
    private int _vendaOrigemId;
    private string? _usuario;
    private List<TrocaItemOrigemModel> _itensOrigem = new();
    private ProdutoDto? _produtoNovoSelecionado;
    private List<ProdutoDto> _resultadosBusca = new();
    private CancellationTokenSource? _buscaCts;
    private bool _suprimirBuscaTextChanged;

    public TrocaFormView(ITrocaService trocaService, IProdutoService produtoService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _trocaService = trocaService;
        _produtoService = produtoService;
    }

    public void Inicializar(VendaDto venda, IReadOnlyList<ItemVendaDto>? itens, string? usuario = null)
    {
        ArgumentNullException.ThrowIfNull(venda);

        _modoVendaExterna = false;
        _vendaOrigemId = venda.Id;
        _usuario = usuario ?? "Operador";

        _itensOrigem = itens?
            .Where(i => i is not null)
            .Select(i => new TrocaItemOrigemModel
            {
                Id = i.Id,
                NomeExibicao = i.DescricaoTroca,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                ProdutoId = i.ProdutoId
            })
            .ToList() ?? [];

        if (_itensOrigem.Count == 0)
            throw new InvalidOperationException("A venda selecionada não possui itens disponíveis para troca.");

        TxtSubtitulo.Text = $"Venda #{venda.NumeroVenda} — {venda.DataVenda:dd/MM/yyyy HH:mm}";

        CmbItemOrigem.ItemsSource = _itensOrigem;
        CmbItemOrigem.SelectedIndex = _itensOrigem.Count > 0 ? 0 : -1;

        AtualizarInfoItemDevolvido();
        AtualizarResumo();
    }

    public void InicializarVendaExterna(
        VendaExternaDto venda,
        IReadOnlyList<ItemVendaExternaDto>? itens,
        string? usuario = null)
    {
        ArgumentNullException.ThrowIfNull(venda);

        _modoVendaExterna = true;
        _vendaOrigemId = venda.Id;
        _usuario = usuario ?? "Operador";

        _itensOrigem = itens?
            .Where(i => i is not null && i.ProdutoId.HasValue)
            .Select(i => new TrocaItemOrigemModel
            {
                Id = i.Id,
                NomeExibicao = i.DescricaoTroca,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                ProdutoId = i.ProdutoId
            })
            .ToList() ?? [];

        if (_itensOrigem.Count == 0)
            throw new InvalidOperationException("A venda externa não possui itens vinculados ao estoque para troca.");

        TxtSubtitulo.Text = $"Venda Externa {venda.NumeroVendaExterna} — {venda.DataVenda:dd/MM/yyyy HH:mm}";

        CmbItemOrigem.ItemsSource = _itensOrigem;
        CmbItemOrigem.SelectedIndex = _itensOrigem.Count > 0 ? 0 : -1;

        AtualizarInfoItemDevolvido();
        AtualizarResumo();
    }

    private void CmbItemOrigem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AtualizarInfoItemDevolvido();
        AtualizarResumo();
    }

    private void AtualizarInfoItemDevolvido()
    {
        if (TxtInfoItemDevolvido is null || CmbItemOrigem is null)
            return;

        if (CmbItemOrigem.SelectedItem is not TrocaItemOrigemModel item)
        {
            TxtInfoItemDevolvido.Text = string.Empty;
            return;
        }

        TxtInfoItemDevolvido.Text =
            $"Preço unitário: {FormattingHelper.FormatarMoeda(item.PrecoUnitario)}  |  Subtotal: {FormattingHelper.FormatarMoeda(item.Subtotal)}";
    }

    private async void TxtBuscaNovoProduto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirBuscaTextChanged) return;

        var termo = TxtBuscaNovoProduto.Text.Trim();
        if (termo.Length < 2)
        {
            PopupResultados.IsOpen = false;
            _produtoNovoSelecionado = null;
            TxtInfoNovoProduto.Text = string.Empty;
            TxtPrecoNovoProduto.Text = string.Empty;
            AtualizarResumo();
            return;
        }

        _buscaCts?.Cancel();
        _buscaCts = new CancellationTokenSource();
        var token = _buscaCts.Token;

        try
        {
            await Task.Delay(200, token);
            var resultados = (await _produtoService.BuscarAsync(termo)).ToList();
            if (token.IsCancellationRequested) return;

            _resultadosBusca = resultados;
            LstResultados.ItemsSource = _resultadosBusca;
            PopupResultados.IsOpen = _resultadosBusca.Count > 0;
        }
        catch (OperationCanceledException) { }
    }

    private void TxtBuscaNovoProduto_KeyDown(object sender, KeyEventArgs e)
    {
        if (!PopupResultados.IsOpen) return;

        if (e.Key == Key.Down)
        {
            LstResultados.Focus();
            if (LstResultados.Items.Count > 0)
                LstResultados.SelectedIndex = 0;
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            PopupResultados.IsOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && LstResultados.SelectedItem is ProdutoDto p)
        {
            SelecionarNovoProduto(p);
            e.Handled = true;
        }
    }

    private void LstResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstResultados.SelectedItem is not ProdutoDto produto) return;
        if (Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.Up)) return;
        SelecionarNovoProduto(produto);
    }

    private void LstResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstResultados.SelectedItem is ProdutoDto produto)
            SelecionarNovoProduto(produto);
    }

    private void SelecionarNovoProduto(ProdutoDto produto)
    {
        _produtoNovoSelecionado = produto;
        PopupResultados.IsOpen = false;

        _suprimirBuscaTextChanged = true;
        TxtBuscaNovoProduto.Text = produto.NomeExibicao;
        _suprimirBuscaTextChanged = false;

        TxtPrecoNovoProduto.Text = FormattingHelper.FormatarMoedaEntrada(produto.PrecoVenda);
        TxtInfoNovoProduto.Text = $"Estoque disponível: {produto.QuantidadeEstoque} {produto.Unidade}";
        AtualizarResumo();
    }

    private void Valores_Changed(object sender, RoutedEventArgs e)
        => AtualizarResumo();

    private void AtualizarResumo()
    {
        if (CmbItemOrigem is null || TxtQtdDevolvida is null || TxtQtdNova is null ||
            TxtPrecoNovoProduto is null || TxtTotalDevolvido is null || TxtTotalNovo is null ||
            TxtDiferencaLabel is null || TxtDiferencaValor is null || BordaDiferenca is null ||
            PainelFormaPagamento is null)
            return;

        var itemDev = CmbItemOrigem.SelectedItem as TrocaItemOrigemModel;

        FormattingHelper.TryParseQuantidade(TxtQtdDevolvida.Text, out var qtdDev);
        FormattingHelper.TryParseQuantidade(TxtQtdNova.Text, out var qtdNova);
        FormattingHelper.TryParseMoeda(TxtPrecoNovoProduto.Text, out var precoNovo);

        var totalDev = (itemDev?.PrecoUnitario ?? 0) * qtdDev;
        var totalNovo = precoNovo * qtdNova;
        var diferenca = totalNovo - totalDev;

        TxtTotalDevolvido.Text = FormattingHelper.FormatarMoeda(totalDev);
        TxtTotalNovo.Text = FormattingHelper.FormatarMoeda(totalNovo);

        if (diferenca == 0)
        {
            TxtDiferencaLabel.Text = "✔ Troca Idêntica";
            TxtDiferencaValor.Text = "R$ 0,00";
            BordaDiferenca.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
            TxtDiferencaLabel.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
            TxtDiferencaValor.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
            PainelFormaPagamento.Visibility = Visibility.Collapsed;
        }
        else if (diferenca > 0)
        {
            TxtDiferencaLabel.Text = "⬆ Diferença a Receber do Cliente";
            TxtDiferencaValor.Text = FormattingHelper.FormatarMoeda(diferenca);
            BordaDiferenca.Background = new SolidColorBrush(Color.FromRgb(255, 243, 205));
            TxtDiferencaLabel.Foreground = new SolidColorBrush(Color.FromRgb(133, 100, 4));
            TxtDiferencaValor.Foreground = new SolidColorBrush(Color.FromRgb(133, 100, 4));
            PainelFormaPagamento.Visibility = Visibility.Visible;
        }
        else
        {
            TxtDiferencaLabel.Text = "⬇ Diferença a Devolver ao Cliente";
            TxtDiferencaValor.Text = FormattingHelper.FormatarMoeda(Math.Abs(diferenca));
            BordaDiferenca.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
            TxtDiferencaLabel.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
            TxtDiferencaValor.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
            PainelFormaPagamento.Visibility = Visibility.Collapsed;
        }
    }

    private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        LimparErro();
        if (!ValidarFormulario()) return;

        var itemDev = (TrocaItemOrigemModel)CmbItemOrigem.SelectedItem!;
        FormattingHelper.TryParseQuantidade(TxtQtdDevolvida.Text, out var qtdDev);
        FormattingHelper.TryParseQuantidade(TxtQtdNova.Text, out var qtdNova);
        FormattingHelper.TryParseMoeda(TxtPrecoNovoProduto.Text, out var precoNovo);

        var formaPagamento = ObterFormaPagamentoSelecionada();

        BtnConfirmar.IsEnabled = false;
        BtnConfirmar.Content = "Processando...";

        try
        {
            if (_modoVendaExterna)
            {
                await _trocaService.RegistrarVendaExternaAsync(new RegistrarTrocaVendaExternaDto
                {
                    VendaExternaOrigemId = _vendaOrigemId,
                    ItemVendaExternaOrigemId = itemDev.Id,
                    QuantidadeDevolvida = qtdDev,
                    RetornarAoEstoque = ChkRetornarEstoque.IsChecked == true,
                    ProdutoNovoId = _produtoNovoSelecionado!.Id,
                    QuantidadeNova = qtdNova,
                    PrecoUnitarioNovo = precoNovo,
                    FormaPagamentoDiferenca = formaPagamento,
                    Usuario = _usuario
                });
            }
            else
            {
                await _trocaService.RegistrarAsync(new RegistrarTrocaDto
                {
                    VendaOrigemId = _vendaOrigemId,
                    ItemVendaOrigemId = itemDev.Id,
                    QuantidadeDevolvida = qtdDev,
                    RetornarAoEstoque = ChkRetornarEstoque.IsChecked == true,
                    ProdutoNovoId = _produtoNovoSelecionado!.Id,
                    QuantidadeNova = qtdNova,
                    PrecoUnitarioNovo = precoNovo,
                    FormaPagamentoDiferenca = formaPagamento,
                    Usuario = _usuario
                });
            }

            DialogResult = true;
            Close();
        }
        catch (DomainException ex)
        {
            ExibirErro(ex.Message);
        }
        catch (Exception ex)
        {
            ExibirErro($"Erro inesperado: {ex.Message}");
        }
        finally
        {
            BtnConfirmar.IsEnabled = true;
            BtnConfirmar.Content = "✔ Confirmar Troca";
        }
    }

    private bool ValidarFormulario()
    {
        if (CmbItemOrigem.SelectedItem is not TrocaItemOrigemModel itemDev)
        {
            ExibirErro("Selecione o item que está sendo devolvido.");
            return false;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQtdDevolvida.Text, out var qtdDev) || qtdDev <= 0)
        {
            ExibirErro("Informe uma quantidade válida para o item devolvido.");
            return false;
        }

        if (qtdDev > itemDev.Quantidade)
        {
            ExibirErro($"A quantidade devolvida ({qtdDev}) não pode ser maior que a quantidade da venda ({itemDev.Quantidade}).");
            return false;
        }

        if (_produtoNovoSelecionado is null)
        {
            ExibirErro("Selecione o novo produto que o cliente está levando.");
            return false;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQtdNova.Text, out var qtdNova) || qtdNova <= 0)
        {
            ExibirErro("Informe uma quantidade válida para o novo item.");
            return false;
        }

        if (!FormattingHelper.TryParseMoeda(TxtPrecoNovoProduto.Text, out var preco) || preco <= 0)
        {
            ExibirErro("Preço do novo produto inválido.");
            return false;
        }

        return true;
    }

    private FormaPagamento? ObterFormaPagamentoSelecionada()
    {
        if (PainelFormaPagamento.Visibility != Visibility.Visible) return null;
        if (RbPix.IsChecked == true) return FormaPagamento.Pix;
        if (RbCartaoDebito.IsChecked == true) return FormaPagamento.CartaoDebito;
        if (RbCartaoCredito.IsChecked == true) return FormaPagamento.CartaoCredito;
        return FormaPagamento.Dinheiro;
    }

    private void ExibirErro(string mensagem)
    {
        TxtErro.Text = mensagem;
        TxtErro.Visibility = Visibility.Visible;
        MessageBox.Show(mensagem, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void LimparErro()
    {
        TxtErro.Text = string.Empty;
        TxtErro.Visibility = Visibility.Collapsed;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
