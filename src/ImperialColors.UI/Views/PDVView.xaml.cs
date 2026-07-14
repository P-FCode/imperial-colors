using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class PDVView : Window, INotifyPropertyChanged
{
    private enum EtapaPdv { Produtos, Cliente, Pagamento }

    private readonly IProdutoService _produtoService;
    private readonly IVendaService _vendaService;
    private readonly IClienteService _clienteService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ISessaoService _sessaoService;

    public event PropertyChangedEventHandler? PropertyChanged;

    private EtapaPdv _etapaAtual = EtapaPdv.Produtos;
    private bool _suprimirTrocaEtapa;
    private bool _uiPronta;

    private TipoDescontoVenda _tipoDesconto = TipoDescontoVenda.Valor;
    private decimal _descontoEmReais;
    private List<ItemVendaDto> _indiceItensPorProdutoId = new();
    private ObservableCollection<ItemVendaDto> _itensVenda = new();
    public ObservableCollection<ItemVendaDto> ItensVenda
    {
        get => _itensVenda;
        set { _itensVenda = value; NotifyPropertyChanged(); AtualizarTotais(); }
    }

    private decimal _subtotal;
    public decimal Subtotal { get => _subtotal; set { _subtotal = value; NotifyPropertyChanged(); } }

    private decimal _total;
    public decimal Total { get => _total; set { _total = value; NotifyPropertyChanged(); } }

    private int _totalItens;
    public int TotalItens { get => _totalItens; set { _totalItens = value; NotifyPropertyChanged(); } }

    private List<ProdutoDto> _produtosEncontrados = new();
    private List<ClienteDto> _clientesEncontrados = new();
    private ClienteDto? _clienteSelecionado;
    private CancellationTokenSource? _leitorBarrasCts;
    private bool _suprimirBuscaTextChanged;
    private bool _suprimirMascaraDocumento;

    private readonly ObservableCollection<PagamentoLinhaUi> _pagamentos = new();
    private FormaPagamento _formaPagamentoSelecionada = FormaPagamento.Dinheiro;

    private const int AtrasoLeitorBarrasMs = 150;

    public PDVView(
        IProdutoService produtoService,
        IVendaService vendaService,
        IClienteService clienteService,
        IServiceProvider serviceProvider,
        ISessaoService sessaoService)
    {
        InitializeComponent();
        DataContext = this;
        _produtoService = produtoService;
        _vendaService = vendaService;
        _clienteService = clienteService;
        _serviceProvider = serviceProvider;
        _sessaoService = sessaoService;

        DgPagamentos.ItemsSource = _pagamentos;
        for (var i = 1; i <= 12; i++)
            CmbParcelas.Items.Add($"{i}x");
        CmbParcelas.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            _uiPronta = true;
            _suprimirTrocaEtapa = true;
            RbEtapaProdutos.IsChecked = true;
            RbConsumidorFinal.IsChecked = true;
            _suprimirTrocaEtapa = false;

            SelecionarFormaPagamento(FormaPagamento.Dinheiro, BtnDinheiro);
            IrParaEtapa(EtapaPdv.Produtos, forcar: true);
            AtualizarResumoCliente();
        };
    }

    public void PrepararFocoBusca()
    {
        if (_etapaAtual != EtapaPdv.Produtos)
            return;

        void Focar()
        {
            TxtBuscaProduto.Focus();
            Keyboard.Focus(TxtBuscaProduto);
        }

        if (IsLoaded)
            Dispatcher.BeginInvoke(Focar, System.Windows.Threading.DispatcherPriority.Input);
        else
            Loaded += (_, _) => Dispatcher.BeginInvoke(Focar, System.Windows.Threading.DispatcherPriority.Input);
    }

    #region Navegação entre etapas

    private void EtapaPdv_Checked(object sender, RoutedEventArgs e)
    {
        if (!_uiPronta || _suprimirTrocaEtapa || sender is not RadioButton rb || rb.IsChecked != true)
            return;

        var destino = rb.Name switch
        {
            nameof(RbEtapaProdutos) => EtapaPdv.Produtos,
            nameof(RbEtapaCliente) => EtapaPdv.Cliente,
            nameof(RbEtapaPagamento) => EtapaPdv.Pagamento,
            _ => _etapaAtual
        };

        if (!TentarIrParaEtapa(destino))
        {
            _suprimirTrocaEtapa = true;
            SelecionarRadioEtapa(_etapaAtual);
            _suprimirTrocaEtapa = false;
        }
    }

    private void BtnAcaoPrincipal_Click(object sender, RoutedEventArgs e)
    {
        AtualizarTotais();

        switch (_etapaAtual)
        {
            case EtapaPdv.Produtos:
                IrParaEtapa(EtapaPdv.Cliente);
                break;
            case EtapaPdv.Cliente:
                if (!ValidarIdentificacaoCliente())
                    return;
                IrParaEtapa(EtapaPdv.Pagamento);
                break;
        }
    }

    private bool TentarIrParaEtapa(EtapaPdv destino)
    {
        if (destino != EtapaPdv.Produtos && !ItensVenda.Any())
        {
            MessageBox.Show("Adicione pelo menos um produto antes de avançar.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (destino == EtapaPdv.Pagamento && _etapaAtual == EtapaPdv.Cliente && !ValidarIdentificacaoCliente())
            return false;

        IrParaEtapa(destino);
        return true;
    }

    private void IrParaEtapa(EtapaPdv etapa, bool forcar = false)
    {
        if (!forcar && etapa != EtapaPdv.Produtos && !ItensVenda.Any())
            return;

        _etapaAtual = etapa;

        if (PainelEtapaProdutos is not null)
            PainelEtapaProdutos.Visibility = etapa == EtapaPdv.Produtos ? Visibility.Visible : Visibility.Collapsed;
        if (PainelEtapaCliente is not null)
            PainelEtapaCliente.Visibility = etapa == EtapaPdv.Cliente ? Visibility.Visible : Visibility.Collapsed;
        if (PainelEtapaPagamento is not null)
            PainelEtapaPagamento.Visibility = etapa == EtapaPdv.Pagamento ? Visibility.Visible : Visibility.Collapsed;

        PopupResultados.IsOpen = false;

        _suprimirTrocaEtapa = true;
        SelecionarRadioEtapa(etapa);
        _suprimirTrocaEtapa = false;

        AtualizarBotoesEtapa();
        AtualizarResumoEtapa();

        if (etapa == EtapaPdv.Pagamento)
        {
            OcultarAvisoPagamento();
            AtualizarTotais();
            TxtPagamentoTotal.Text = FormattingHelper.FormatarMoeda(Total);
            AtualizarResumoPagamentos();
            PreencherValorPagamentoSugerido();
        }
        else if (etapa == EtapaPdv.Produtos)
        {
            PrepararFocoBusca();
        }
    }

    private void SelecionarRadioEtapa(EtapaPdv etapa)
    {
        if (RbEtapaProdutos is null || RbEtapaCliente is null || RbEtapaPagamento is null)
            return;

        RbEtapaProdutos.IsChecked = etapa == EtapaPdv.Produtos;
        RbEtapaCliente.IsChecked = etapa == EtapaPdv.Cliente;
        RbEtapaPagamento.IsChecked = etapa == EtapaPdv.Pagamento;
    }

    private void AtualizarBotoesEtapa()
    {
        if (BtnAcaoPrincipal is null || BtnConcluirVenda is null)
            return;

        BtnAcaoPrincipal.Visibility = _etapaAtual == EtapaPdv.Pagamento
            ? Visibility.Collapsed
            : Visibility.Visible;

        BtnConcluirVenda.Visibility = _etapaAtual == EtapaPdv.Pagamento
            ? Visibility.Visible
            : Visibility.Collapsed;

        BtnAcaoPrincipal.Content = _etapaAtual switch
        {
            EtapaPdv.Produtos => "Continuar → Cliente",
            EtapaPdv.Cliente => "Continuar → Pagamento",
            _ => "Continuar"
        };
    }

    private void AtualizarResumoEtapa()
    {
        if (TxtResumoEtapa is null)
            return;

        TxtResumoEtapa.Text = _etapaAtual switch
        {
            EtapaPdv.Produtos => "Bipe ou busque os produtos da venda",
            EtapaPdv.Cliente => ObterResumoClienteCurto(),
            EtapaPdv.Pagamento => _pagamentos.Count > 0
                ? $"{_pagamentos.Count} forma(s) de pagamento lançada(s)"
                : "Nenhum pagamento lançado ainda",
            _ => string.Empty
        };
    }

    private string ObterResumoClienteCurto()
    {
        if (RbConsumidorFinal?.IsChecked == true)
            return "Cliente: Consumidor Final";

        if (RbClienteCadastrado?.IsChecked == true && _clienteSelecionado is not null)
            return $"Cliente: {_clienteSelecionado.Nome ?? "Cliente"}";

        if (RbDadosCupom?.IsChecked == true && TxtNomeCupom is not null
            && !string.IsNullOrWhiteSpace(TxtNomeCupom.Text))
            return $"Cliente: {TxtNomeCupom.Text.Trim()}";

        return "Defina o comprador";
    }

    #endregion

    #region Produtos

    private async void TxtBuscaProduto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirBuscaTextChanged || _etapaAtual != EtapaPdv.Produtos)
            return;

        var termo = TxtBuscaProduto.Text.Trim();
        if (termo.Length < 2)
        {
            PopupResultados.IsOpen = false;
            return;
        }

        if (PareceCodigoBarras(termo))
        {
            PopupResultados.IsOpen = false;
            await TentarAutoAdicionarPorCodigoBarrasAsync(termo);
            return;
        }

        _produtosEncontrados = (await _produtoService.BuscarAsync(termo)).ToList();
        if (_produtosEncontrados.Any())
        {
            LstResultados.ItemsSource = _produtosEncontrados;
            PopupResultados.IsOpen = true;
        }
        else
        {
            PopupResultados.IsOpen = false;
        }
    }

    private async void TxtBuscaProduto_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ProcessarEntradaBuscaAsync();
        }

        if (e.Key == Key.Escape)
            PopupResultados.IsOpen = false;
    }

    private async void BtnAdicionarProduto_Click(object sender, RoutedEventArgs e)
        => await ProcessarEntradaBuscaAsync();

    private async Task ProcessarEntradaBuscaAsync()
    {
        CancelarLeituraBarrasPendente();

        var termo = TxtBuscaProduto.Text.Trim();
        if (string.IsNullOrWhiteSpace(termo))
            return;

        var porBarras = await _produtoService.ObterPorCodigoBarrasAsync(termo);
        if (porBarras is not null)
        {
            AdicionarItemVenda(porBarras);
            return;
        }

        var porCodigoInterno = await _produtoService.ObterPorCodigoInternoAsync(termo);
        if (porCodigoInterno is not null)
        {
            AdicionarItemVenda(porCodigoInterno);
            return;
        }

        _produtosEncontrados = (await _produtoService.BuscarAsync(termo)).ToList();

        if (_produtosEncontrados.Count == 1)
        {
            AdicionarItemVenda(_produtosEncontrados[0]);
            return;
        }

        if (_produtosEncontrados.Count > 1)
        {
            if (LstResultados.SelectedItem is ProdutoDto selecionado)
            {
                AdicionarItemVenda(selecionado);
                return;
            }

            LstResultados.ItemsSource = _produtosEncontrados;
            PopupResultados.IsOpen = true;
            return;
        }

        PopupResultados.IsOpen = false;
        NotificarProdutoNaoEncontrado();
    }

    private void NotificarProdutoNaoEncontrado()
    {
        _suprimirBuscaTextChanged = true;
        BarcodeScanHelper.ProdutoNaoEncontrado(TxtBuscaProduto, TxtAvisoBarras);
        _suprimirBuscaTextChanged = false;
    }

    private async Task TentarAutoAdicionarPorCodigoBarrasAsync(string termo)
    {
        CancelarLeituraBarrasPendente();
        _leitorBarrasCts = new CancellationTokenSource();
        var token = _leitorBarrasCts.Token;

        try
        {
            await Task.Delay(AtrasoLeitorBarrasMs, token);
            if (token.IsCancellationRequested)
                return;

            var termoAtual = TxtBuscaProduto.Text.Trim();
            if (!string.Equals(termoAtual, termo, StringComparison.Ordinal))
                return;

            var produto = await _produtoService.ObterPorCodigoBarrasAsync(termoAtual);
            if (produto is not null)
            {
                AdicionarItemVenda(produto);
                return;
            }

            if (PareceCodigoBarras(termoAtual))
                NotificarProdutoNaoEncontrado();
        }
        catch (OperationCanceledException) { }
    }

    private void CancelarLeituraBarrasPendente()
    {
        _leitorBarrasCts?.Cancel();
        _leitorBarrasCts?.Dispose();
        _leitorBarrasCts = null;
    }

    private static bool PareceCodigoBarras(string termo)
        => termo.Length >= 4 && termo.All(char.IsDigit);

    private void LstResultados_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstResultados.SelectedItem is ProdutoDto produto)
        {
            AdicionarItemVenda(produto);
            PopupResultados.IsOpen = false;
        }
    }

    private void AdicionarItemVenda(ProdutoDto? produto)
    {
        if (produto is null)
            return;

        var nomeProduto = string.IsNullOrWhiteSpace(produto.Nome) ? "Produto" : produto.Nome;
        var unidade = string.IsNullOrWhiteSpace(produto.Unidade) ? "UN" : produto.Unidade;
        var precoEfetivo = produto.PrecoEfetivo > 0 ? produto.PrecoEfetivo : produto.PrecoVenda;

        if (produto.SemEstoque)
        {
            MessageBox.Show($"Produto '{nomeProduto}' sem estoque!", "Sem Estoque",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var itemExistente = BuscarItemPorProdutoId(produto.Id);
        if (itemExistente is not null)
        {
            if (itemExistente.Quantidade + 1 > produto.QuantidadeEstoque)
            {
                MessageBox.Show($"Estoque insuficiente! Disponível: {produto.QuantidadeEstoque} {unidade}",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            itemExistente.Quantidade += 1;
            itemExistente.Subtotal = itemExistente.Quantidade * itemExistente.PrecoUnitario - itemExistente.Desconto;
            DgItens?.Items.Refresh();
            ReindexarItensVenda();
        }
        else
        {
            ItensVenda.Add(new ItemVendaDto
            {
                ProdutoId = produto.Id,
                NomeProduto = nomeProduto,
                CodigoInterno = produto.CodigoInterno ?? string.Empty,
                Quantidade = 1,
                PrecoUnitario = precoEfetivo,
                PrecoOriginal = produto.EmPromocao ? produto.PrecoVenda : null,
                EmPromocao = produto.EmPromocao,
                Desconto = 0,
                Subtotal = precoEfetivo,
                Unidade = unidade
            });
            ReindexarItensVenda();
        }

        CancelarLeituraBarrasPendente();
        PopupResultados.IsOpen = false;
        _suprimirBuscaTextChanged = true;
        TxtBuscaProduto.Clear();
        _suprimirBuscaTextChanged = false;
        TxtBuscaProduto.Focus();
        AtualizarTotais();
    }

    private void BtnRemoverItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ItemVendaDto item)
        {
            ItensVenda.Remove(item);
            ReindexarItensVenda();
            AtualizarTotais();
        }
    }

    private void BtnAumentarQtd_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ItemVendaDto item)
        {
            item.Quantidade += 1;
            item.Subtotal = item.Quantidade * item.PrecoUnitario - item.Desconto;
            DgItens.Items.Refresh();
            AtualizarTotais();
        }
    }

    private void BtnDiminuirQtd_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is ItemVendaDto item)
        {
            if (item.Quantidade <= 1)
            {
                ItensVenda.Remove(item);
                ReindexarItensVenda();
            }
            else
            {
                item.Quantidade -= 1;
                item.Subtotal = item.Quantidade * item.PrecoUnitario - item.Desconto;
                DgItens.Items.Refresh();
            }

            AtualizarTotais();
        }
    }

    private void TxtQtdItem_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as TextBox)?.Tag is ItemVendaDto item
            && decimal.TryParse(((TextBox)sender).Text, out var qtd) && qtd > 0)
        {
            item.Quantidade = qtd;
            item.Subtotal = item.Quantidade * item.PrecoUnitario - item.Desconto;
            DgItens.Items.Refresh();
            AtualizarTotais();
        }
    }

    private void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        if (ItensVenda.Any() &&
            MessageBox.Show("Limpar todos os itens?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            ItensVenda.Clear();
            ReindexarItensVenda();
            AtualizarTotais();
        }
    }

    #endregion

    #region Cliente

    private void ModoCliente_Changed(object sender, RoutedEventArgs e)
    {
        if (!_uiPronta || PainelClienteCadastrado is null || PainelDadosCupom is null
            || RbClienteCadastrado is null || RbDadosCupom is null)
            return;

        PainelClienteCadastrado.Visibility = RbClienteCadastrado.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
        PainelDadosCupom.Visibility = RbDadosCupom.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;

        AtualizarResumoCliente();
        AtualizarResumoEtapa();
    }

    private async void TxtBuscaCliente_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (RbClienteCadastrado.IsChecked != true)
            return;

        var termo = TxtBuscaCliente.Text.Trim();
        if (termo.Length < 2)
        {
            LstClientes.ItemsSource = null;
            _clientesEncontrados.Clear();
            _clienteSelecionado = null;
            AtualizarResumoCliente();
            return;
        }

        _clientesEncontrados = (await _clienteService.BuscarAsync(termo)).ToList();
        LstClientes.ItemsSource = _clientesEncontrados;
    }

    private void LstClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _clienteSelecionado = LstClientes.SelectedItem as ClienteDto;
        AtualizarResumoCliente();
        AtualizarResumoEtapa();
    }

    private void TipoPessoaCupom_Changed(object sender, RoutedEventArgs e)
    {
        if (LblDocumentoCupom is null || LblNomeCupom is null)
            return;

        var pj = RbPessoaJuridica.IsChecked == true;
        LblNomeCupom.Text = pj ? "Razão Social *" : "Nome *";
        LblDocumentoCupom.Text = pj ? "CNPJ" : "CPF";
        TxtDocumentoCupom.MaxLength = pj ? 18 : 14;
    }

    private void TxtDocumentoCupom_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirMascaraDocumento)
            return;

        _suprimirMascaraDocumento = true;
        TxtDocumentoCupom.Text = RbPessoaJuridica.IsChecked == true
            ? DocumentoHelper.AplicarMascaraCnpj(TxtDocumentoCupom.Text)
            : DocumentoHelper.AplicarMascaraCpf(TxtDocumentoCupom.Text);
        TxtDocumentoCupom.SelectionStart = TxtDocumentoCupom.Text.Length;
        _suprimirMascaraDocumento = false;
        AtualizarResumoCliente();
    }

    private void AtualizarResumoCliente()
    {
        if (TxtResumoCliente is null)
            return;

        TxtResumoCliente.Text = ObterResumoClienteCurto();
    }

    private bool ValidarIdentificacaoCliente()
    {
        if (RbConsumidorFinal.IsChecked == true)
            return true;

        if (RbClienteCadastrado.IsChecked == true)
        {
            if (_clienteSelecionado is null)
            {
                MessageBox.Show("Selecione um cliente da lista.", "Validação",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        if (string.IsNullOrWhiteSpace(TxtNomeCupom.Text))
        {
            MessageBox.Show("Informe o nome ou razão social do comprador.", "Validação",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    private IdentificacaoClientePdvDto MontarIdentificacaoCliente()
    {
        if (RbConsumidorFinal.IsChecked == true)
            return new IdentificacaoClientePdvDto { ConsumidorFinal = true };

        if (RbClienteCadastrado.IsChecked == true && _clienteSelecionado is not null)
            return new IdentificacaoClientePdvDto { ConsumidorFinal = false, ClienteId = _clienteSelecionado.Id };

        var pj = RbPessoaJuridica.IsChecked == true;
        return new IdentificacaoClientePdvDto
        {
            ConsumidorFinal = false,
            NomeCompradorAvulso = TxtNomeCupom.Text.Trim(),
            DocumentoCompradorAvulso = string.IsNullOrWhiteSpace(TxtDocumentoCupom.Text)
                ? null : TxtDocumentoCupom.Text.Trim(),
            TipoPessoaCompradorAvulso = pj ? TipoPessoa.Juridica : TipoPessoa.Fisica
        };
    }

    #endregion

    #region Pagamento

    private void FormaPagamento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton botao || botao.Tag is not string tag)
            return;

        if (!Enum.TryParse<FormaPagamento>(tag, out var forma))
            return;

        SelecionarFormaPagamento(forma, botao);
    }

    private void SelecionarFormaPagamento(FormaPagamento forma, ToggleButton? botaoAtivo)
    {
        _formaPagamentoSelecionada = forma;

        if (GridFormasPagamento is not null)
        {
            foreach (var child in GridFormasPagamento.Children.OfType<ToggleButton>())
                child.IsChecked = ReferenceEquals(child, botaoAtivo);
        }

        if (PainelRecebido is not null)
            PainelRecebido.Visibility = PagamentoHelper.UsaTroco(forma) ? Visibility.Visible : Visibility.Collapsed;
        if (PainelParcelas is not null)
            PainelParcelas.Visibility = PagamentoHelper.PermiteParcelamento(forma) ? Visibility.Visible : Visibility.Collapsed;

        PreencherValorPagamentoSugerido();
        AtualizarInfoTrocoPagamento();
    }

    private void PreencherValorPagamentoSugerido()
    {
        var saldo = CalcularSaldoRestantePagamento();
        TxtValorPagamento.Text = FormattingHelper.FormatarMoedaEntrada(saldo > 0 ? saldo : 0m);

        if (PagamentoHelper.UsaTroco(_formaPagamentoSelecionada))
            TxtValorRecebido.Text = TxtValorPagamento.Text;
    }

    private void TxtValorPagamento_TextChanged(object sender, TextChangedEventArgs e)
        => AtualizarInfoTrocoPagamento();

    private void TxtValorRecebido_TextChanged(object sender, TextChangedEventArgs e)
        => AtualizarInfoTrocoPagamento();

    private void AtualizarInfoTrocoPagamento()
    {
        if (!PagamentoHelper.UsaTroco(_formaPagamentoSelecionada))
        {
            TxtInfoTroco.Visibility = Visibility.Collapsed;
            return;
        }

        FormattingHelper.TryParseMoeda(TxtValorPagamento.Text, out var valorPagamento);
        FormattingHelper.TryParseMoeda(TxtValorRecebido.Text, out var recebido);

        if (recebido > valorPagamento && valorPagamento > 0)
        {
            TxtInfoTroco.Text = $"Troco deste pagamento: {FormattingHelper.FormatarMoeda(recebido - valorPagamento)}";
            TxtInfoTroco.Visibility = Visibility.Visible;
        }
        else
        {
            TxtInfoTroco.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnAdicionarPagamento_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!FormattingHelper.TryParseMoeda(TxtValorPagamento.Text, out var valor) || valor <= 0)
            {
                MessageBox.Show("Informe um valor válido para o pagamento.", "Validação",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saldo = CalcularSaldoRestantePagamento();
            if (valor > saldo)
            {
                MessageBox.Show($"O valor não pode exceder o saldo restante ({FormattingHelper.FormatarMoeda(saldo)}).",
                    "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? valorRecebido = null;
            var parcelas = 1;

            if (PagamentoHelper.UsaTroco(_formaPagamentoSelecionada))
            {
                if (!FormattingHelper.TryParseMoeda(TxtValorRecebido.Text, out var recebido) || recebido < valor)
                {
                    MessageBox.Show("Valor recebido em espécie insuficiente.", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                valorRecebido = recebido;
            }
            else if (PagamentoHelper.PermiteParcelamento(_formaPagamentoSelecionada))
            {
                parcelas = CmbParcelas.SelectedIndex >= 0 ? CmbParcelas.SelectedIndex + 1 : 1;
            }

            _pagamentos.Add(new PagamentoLinhaUi
            {
                Forma = _formaPagamentoSelecionada,
                Valor = valor,
                ValorRecebido = valorRecebido,
                Parcelas = parcelas
            });

            AtualizarResumoPagamentos();
            PreencherValorPagamentoSugerido();
            AtualizarResumoEtapa();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnRemoverPagamento_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is PagamentoLinhaUi linha)
        {
            _pagamentos.Remove(linha);
            AtualizarResumoPagamentos();
            PreencherValorPagamentoSugerido();
            AtualizarResumoEtapa();
        }
    }

    private decimal CalcularSaldoRestantePagamento()
        => Math.Max(0, Total - _pagamentos.Sum(p => p.Valor));

    private void AtualizarResumoPagamentos()
    {
        var totalPago = _pagamentos.Sum(p => p.Valor);
        var saldo = Math.Max(0, Total - totalPago);

        TxtPagamentoTotal.Text = FormattingHelper.FormatarMoeda(Total);
        TxtTotalPago.Text = FormattingHelper.FormatarMoeda(totalPago);
        TxtSaldoRestante.Text = FormattingHelper.FormatarMoeda(saldo);
        BtnConcluirVenda.IsEnabled = saldo == 0 && _pagamentos.Count > 0;
    }

    #endregion

    #region Totais e finalização

    private void TxtDesconto_TextChanged(object sender, TextChangedEventArgs e) => AtualizarTotais();

    private void CmbTipoDesconto_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTipoDesconto?.SelectedItem is not ComboBoxItem item)
            return;

        _tipoDesconto = item.Content?.ToString() == "%" ? TipoDescontoVenda.Percentual : TipoDescontoVenda.Valor;
        AtualizarTotais();
    }

    private void AtualizarTotais()
    {
        Subtotal = ItensVenda.Sum(i => i.Subtotal);

        var valorInformado = ObterValorDescontoInformado();
        if (_tipoDesconto == TipoDescontoVenda.Percentual && !DescontoHelper.PercentualValido(valorInformado))
            valorInformado = Math.Clamp(valorInformado, 0m, DescontoHelper.PercentualMaximo);

        _descontoEmReais = DescontoHelper.CalcularDescontoEmReais(Subtotal, valorInformado, _tipoDesconto);
        Total = DescontoHelper.CalcularTotalLiquido(Subtotal, _descontoEmReais);
        TotalItens = ItensVenda.Count;

        if (TxtDescontoEquivalente is not null)
        {
            TxtDescontoEquivalente.Text = _tipoDesconto == TipoDescontoVenda.Percentual && Subtotal > 0
                ? $"Equivale a {FormattingHelper.FormatarMoeda(_descontoEmReais)}"
                : string.Empty;
        }

        if (_etapaAtual == EtapaPdv.Pagamento)
        {
            // Pagamentos lançados que excedem o novo total precisam ser reconciliados
            if (_pagamentos.Count > 0 && _pagamentos.Sum(p => p.Valor) > Total)
                ReconciliarPagamentosComNovoTotal();
            else
            {
                OcultarAvisoPagamento();
                AtualizarResumoPagamentos();
            }
        }
    }

    /// <summary>
    /// Chamado quando o Total muda (desconto alterado) enquanto há pagamentos já lançados.
    /// Regras: não-dinheiro com valor > saldo disponível → remove;
    ///         dinheiro → ajusta o Valor ao saldo restante se ValorRecebido cobrir.
    /// </summary>
    private void ReconciliarPagamentosComNovoTotal()
    {
        var novoTotal = Total;
        var saldoRestante = novoTotal;
        var reconciliados = new List<PagamentoLinhaUi>();
        var removidos = 0;
        var ajustados = 0;

        foreach (var pag in _pagamentos)
        {
            if (saldoRestante <= 0)
            {
                removidos++;
                continue;
            }

            if (PagamentoHelper.UsaTroco(pag.Forma))
            {
                // Dinheiro: mantém se o valor recebido em espécie cobre o saldo restante
                var valorRecebido = pag.ValorRecebido ?? pag.Valor;
                if (valorRecebido >= saldoRestante)
                {
                    var novoValor = Math.Min(pag.Valor, saldoRestante);
                    if (novoValor != pag.Valor)
                    {
                        // Recalcula o Valor do item de dinheiro; troco será maior
                        reconciliados.Add(new PagamentoLinhaUi
                        {
                            Forma = pag.Forma,
                            Valor = novoValor,
                            ValorRecebido = valorRecebido,
                            Parcelas = pag.Parcelas
                        });
                        ajustados++;
                    }
                    else
                    {
                        reconciliados.Add(pag);
                    }

                    saldoRestante -= novoValor;
                }
                else
                {
                    // Valor recebido insuficiente até para o saldo restante → remove
                    removidos++;
                }
            }
            else
            {
                // Cartão, PIX, Boleto: não permitem troco
                // Remove se excede o saldo disponível; mantém caso contrário
                if (pag.Valor <= saldoRestante)
                {
                    reconciliados.Add(pag);
                    saldoRestante -= pag.Valor;
                }
                else
                {
                    removidos++;
                }
            }
        }

        // Reconstrói a coleção observável
        _pagamentos.Clear();
        foreach (var pag in reconciliados)
            _pagamentos.Add(pag);

        // Exibe aviso contextual ao operador
        if (removidos > 0 || ajustados > 0)
        {
            var aviso = removidos > 0
                ? $"O desconto alterou o total da venda. {removidos} pagamento(s) removido(s) — relance com o novo total."
                : "O desconto alterou o total. O troco em dinheiro foi recalculado automaticamente.";
            ExibirAvisoPagamento(aviso);
        }
        else
        {
            OcultarAvisoPagamento();
        }

        AtualizarResumoPagamentos();
        PreencherValorPagamentoSugerido();
        AtualizarResumoEtapa();
    }

    private void ExibirAvisoPagamento(string mensagem)
    {
        if (TxtAvisoPagamento is null || PainelAvisoPagamento is null) return;
        TxtAvisoPagamento.Text = mensagem;
        PainelAvisoPagamento.Visibility = Visibility.Visible;
    }

    private void OcultarAvisoPagamento()
    {
        if (PainelAvisoPagamento is null) return;
        PainelAvisoPagamento.Visibility = Visibility.Collapsed;
    }

    private decimal ObterValorDescontoInformado()
    {
        var texto = TxtDesconto?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            return 0m;

        if (_tipoDesconto == TipoDescontoVenda.Valor &&
            FormattingHelper.TryParseMoeda(texto, out var moeda))
            return moeda;

        if (decimal.TryParse(texto.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var numero))
            return numero;

        return 0m;
    }

    private async void BtnConcluirVenda_Click(object sender, RoutedEventArgs e)
    {
        if (!ItensVenda.Any())
        {
            MessageBox.Show("Adicione pelo menos um produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidarIdentificacaoCliente())
        {
            IrParaEtapa(EtapaPdv.Cliente);
            return;
        }

        try
        {
            AtualizarTotais();

            var pagamentos = _pagamentos.Select(p => new CriarVendaPagamentoDto
            {
                FormaPagamento = p.Forma,
                Valor = p.Valor,
                ValorRecebido = p.ValorRecebido,
                QuantidadeParcelas = p.Parcelas
            }).ToList();

            PagamentoHelper.ValidarPagamentosCompostos(Total, pagamentos);

            var idCliente = MontarIdentificacaoCliente();
            var dto = new CriarVendaDto
            {
                ClienteId = idCliente.ClienteId,
                ConsumidorFinal = idCliente.ConsumidorFinal,
                NomeCompradorAvulso = idCliente.NomeCompradorAvulso,
                DocumentoCompradorAvulso = idCliente.DocumentoCompradorAvulso,
                TipoPessoaCompradorAvulso = idCliente.TipoPessoaCompradorAvulso,
                Desconto = _descontoEmReais,
                Observacoes = TxtObservacoes.Text,
                Usuario = _sessaoService.ObterNomeUsuario(),
                Pagamentos = pagamentos,
                Itens = ItensVenda.Select(i => new CriarItemVendaDto
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    Desconto = i.Desconto
                }).ToList()
            };

            var venda = await _vendaService.CriarAsync(dto);
            await WindowHelper.ExibirCupomAsync(_serviceProvider, venda, Owner);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao finalizar venda: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
    {
        if (!ItensVenda.Any() ||
            MessageBox.Show("Cancelar esta venda?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            DialogResult = false;
            Close();
        }
    }

    #endregion

    private void ReindexarItensVenda()
        => _indiceItensPorProdutoId = BinarySearchCollectionHelper.OrdenarPorId(ItensVenda, i => i.ProdutoId);

    private ItemVendaDto? BuscarItemPorProdutoId(int produtoId)
        => BinarySearchCollectionHelper.FindById(_indiceItensPorProdutoId, produtoId, i => i.ProdutoId);

    private void NotifyPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private sealed class PagamentoLinhaUi
    {
        public FormaPagamento Forma { get; init; }
        public decimal Valor { get; init; }
        public decimal? ValorRecebido { get; init; }
        public int Parcelas { get; init; } = 1;
        public string Descricao => PagamentoHelper.ObterDescricao(Forma, Parcelas);
        public string ValorFormatado => FormattingHelper.FormatarMoeda(Valor);
        public string TrocoFormatado
        {
            get
            {
                if (!PagamentoHelper.UsaTroco(Forma))
                    return "—";
                var troco = Math.Max(0, (ValorRecebido ?? Valor) - Valor);
                return troco > 0 ? FormattingHelper.FormatarMoeda(troco) : "—";
            }
        }
    }
}
