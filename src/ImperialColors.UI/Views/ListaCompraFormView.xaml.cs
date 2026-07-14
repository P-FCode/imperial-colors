using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class ListaCompraFormView : Window
{
    private readonly IListaCompraService _listaCompraService;
    private readonly IFornecedorService _fornecedorService;
    private readonly IProdutoService _produtoService;
    private int _listaId;
    private List<ProdutoDto> _produtosEncontrados = new();
    private readonly ObservableCollection<ItemListaCompraFormModel> _itens = new();

    public ListaCompraFormView(
        IListaCompraService listaCompraService,
        IFornecedorService fornecedorService,
        IProdutoService produtoService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _listaCompraService = listaCompraService;
        _fornecedorService = fornecedorService;
        _produtoService = produtoService;
        GridItens.ItemsSource = _itens;
        Loaded += async (_, _) => await CarregarCombosAsync();
    }

    public void InicializarNova()
    {
        TxtTitulo.Text = "Nova Lista de Compras";
        _listaId = 0;
        TxtNome.Text = string.Empty;
        TxtObservacoes.Text = string.Empty;
        CmbFornecedor.SelectedIndex = -1;
        RbModoEstoque.IsChecked = true;
        LimparCamposItem();
        _itens.Clear();
        LimparErroValidacao();
    }

    public void InicializarEdicao(ListaCompraDto lista)
    {
        ArgumentNullException.ThrowIfNull(lista);

        TxtTitulo.Text = "Editar Lista de Compras";
        _listaId = lista.Id;
        TxtNome.Text = lista.Nome ?? string.Empty;
        TxtObservacoes.Text = lista.Observacoes ?? string.Empty;
        _itens.Clear();

        foreach (var item in lista.Itens ?? [])
        {
            _itens.Add(new ItemListaCompraFormModel
            {
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto ?? string.Empty,
                EhManual = item.ItemManual,
                Unidade = item.Unidade ?? "UN",
                QuantidadeDesejada = item.QuantidadeDesejada,
                QuantidadeComprada = item.QuantidadeComprada,
                Comprado = item.Comprado,
                Observacoes = item.Observacoes
            });
        }

        RbModoEstoque.IsChecked = true;
        LimparCamposItem();
        _ = CarregarCombosAsync(lista.FornecedorId);
        LimparErroValidacao();
    }

    private async Task CarregarCombosAsync(int? fornecedorId = null)
    {
        var fornecedores = (await _fornecedorService.ObterTodosAsync()).ToList();
        CmbFornecedor.ItemsSource = fornecedores;

        if (fornecedorId.HasValue)
            CmbFornecedor.SelectedValue = fornecedorId;
        else
            CmbFornecedor.SelectedIndex = -1;
    }

    private void ModoItem_Changed(object sender, RoutedEventArgs e)
    {
        if (PainelModoEstoque is null || PainelModoManual is null)
            return;

        var estoque = RbModoEstoque.IsChecked == true;
        PainelModoEstoque.Visibility = estoque ? Visibility.Visible : Visibility.Collapsed;
        PainelModoManual.Visibility = estoque ? Visibility.Collapsed : Visibility.Visible;
        PopupResultadosProduto.IsOpen = false;
        LimparErroValidacao();
    }

    private async void TxtBuscaProduto_TextChanged(object sender, TextChangedEventArgs e)
    {
        var termo = TxtBuscaProduto.Text.Trim();
        if (termo.Length < 2)
        {
            PopupResultadosProduto.IsOpen = false;
            return;
        }

        _produtosEncontrados = (await _produtoService.BuscarAsync(termo)).ToList();
        if (_produtosEncontrados.Count > 0)
        {
            LstResultadosProduto.ItemsSource = _produtosEncontrados;
            PopupResultadosProduto.IsOpen = true;
        }
        else
        {
            PopupResultadosProduto.IsOpen = false;
        }
    }

    private async void TxtBuscaProduto_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await AdicionarProdutoDoEstoqueAsync();
        }
        else if (e.Key == Key.Escape)
        {
            PopupResultadosProduto.IsOpen = false;
        }
    }

    private void LstResultadosProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstResultadosProduto.SelectedItem is ProdutoDto produto)
            TxtBuscaProduto.Text = produto.Nome;
    }

    private async void LstResultadosProduto_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstResultadosProduto.SelectedItem is ProdutoDto produto)
        {
            TxtBuscaProduto.Text = produto.Nome;
            await AdicionarProdutoDoEstoqueAsync(produto);
        }
    }

    private async void BtnAdicionarItemEstoque_Click(object sender, RoutedEventArgs e)
        => await AdicionarProdutoDoEstoqueAsync();

    private async Task AdicionarProdutoDoEstoqueAsync(ProdutoDto? produtoSelecionado = null)
    {
        LimparErroValidacao();
        PopupResultadosProduto.IsOpen = false;

        ProdutoDto? produto = produtoSelecionado;

        if (produto is null)
        {
            var termo = TxtBuscaProduto.Text.Trim();
            if (string.IsNullOrWhiteSpace(termo))
            {
                ExibirErroValidacao("Digite o nome, código ou código de barras do produto.");
                return;
            }

            produto = await _produtoService.ObterPorCodigoBarrasAsync(termo)
                      ?? await _produtoService.ObterPorCodigoInternoAsync(termo);

            if (produto is null)
            {
                var resultados = (await _produtoService.BuscarAsync(termo)).ToList();
                if (resultados.Count == 1)
                    produto = resultados[0];
                else if (LstResultadosProduto.SelectedItem is ProdutoDto selecionado)
                    produto = selecionado;
                else if (resultados.Count > 1)
                {
                    LstResultadosProduto.ItemsSource = resultados;
                    PopupResultadosProduto.IsOpen = true;
                    ExibirErroValidacao("Selecione um produto na lista de resultados.");
                    return;
                }
            }
        }

        if (produto is null)
        {
            ExibirErroValidacao("Produto não encontrado no estoque.");
            return;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidadeEstoque.Text, out var quantidade) || quantidade <= 0)
        {
            ExibirErroValidacao("Informe uma quantidade válida.");
            return;
        }

        if (_itens.Any(i => !i.EhManual && i.ProdutoId == produto.Id))
        {
            ExibirErroValidacao("Este produto já está na lista.");
            return;
        }

        _itens.Add(new ItemListaCompraFormModel
        {
            ProdutoId = produto.Id,
            NomeProduto = produto.Nome,
            EhManual = false,
            Unidade = produto.Unidade,
            QuantidadeDesejada = quantidade,
            QuantidadeComprada = null,
            Comprado = false
        });

        LimparCamposItem();
    }

    private void BtnAdicionarItemManual_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        var descricao = TxtDescricaoManual.Text.Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            ExibirErroValidacao("Informe a descrição do item.");
            return;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidadeManual.Text, out var quantidade) || quantidade <= 0)
        {
            ExibirErroValidacao("Informe uma quantidade válida.");
            return;
        }

        if (_itens.Any(i => i.EhManual && string.Equals(i.NomeProduto, descricao, StringComparison.OrdinalIgnoreCase)))
        {
            ExibirErroValidacao("Este item manual já está na lista.");
            return;
        }

        _itens.Add(new ItemListaCompraFormModel
        {
            ProdutoId = null,
            NomeProduto = descricao,
            EhManual = true,
            Unidade = "UN",
            QuantidadeDesejada = quantidade,
            QuantidadeComprada = null,
            Comprado = false
        });

        TxtDescricaoManual.Text = string.Empty;
        TxtQuantidadeManual.Text = "1";
    }

    private void LimparCamposItem()
    {
        TxtBuscaProduto.Text = string.Empty;
        TxtQuantidadeEstoque.Text = "1";
        TxtDescricaoManual.Text = string.Empty;
        TxtQuantidadeManual.Text = "1";
        PopupResultadosProduto.IsOpen = false;
    }

    private void BtnRemoverItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ItemListaCompraFormModel item }) return;
        _itens.Remove(item);
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        if (string.IsNullOrWhiteSpace(TxtNome.Text))
        {
            ExibirErroValidacao("Informe o nome da lista.");
            return;
        }

        if (_itens.Count == 0)
        {
            ExibirErroValidacao("Adicione pelo menos um item à lista.");
            return;
        }

        try
        {
            BtnSalvar.IsEnabled = false;
            BtnSalvar.Content = "Salvando...";

            int? fornecedorId = null;
            if (CmbFornecedor.SelectedValue is int id)
                fornecedorId = id;

            var dto = new SalvarListaCompraDto
            {
                Id = _listaId,
                Nome = TxtNome.Text.Trim(),
                FornecedorId = fornecedorId,
                Observacoes = string.IsNullOrWhiteSpace(TxtObservacoes.Text) ? null : TxtObservacoes.Text.Trim(),
                Itens = _itens.Select(i => new SalvarItemListaCompraDto
                {
                    ProdutoId = i.EhManual ? null : i.ProdutoId,
                    NomeManual = i.EhManual ? i.NomeProduto : null,
                    QuantidadeDesejada = i.QuantidadeDesejada,
                    QuantidadeComprada = i.QuantidadeComprada,
                    Comprado = i.Comprado,
                    Observacoes = i.Observacoes
                }).ToList()
            };

            await _listaCompraService.SalvarAsync(dto);
            DialogResult = true;
            Close();
        }
        catch (DomainException ex)
        {
            ExibirErroValidacao(ex.Message);
        }
        catch (Exception ex)
        {
            ExibirErroValidacao(ExceptionMessageHelper.ObterMensagemAmigavel(ex));
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar Lista";
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LimparErroValidacao()
    {
        TxtErroValidacao.Text = string.Empty;
        TxtErroValidacao.Visibility = Visibility.Collapsed;
    }

    private void ExibirErroValidacao(string mensagem)
    {
        TxtErroValidacao.Text = mensagem;
        TxtErroValidacao.Visibility = Visibility.Visible;
    }

    private sealed class ItemListaCompraFormModel : INotifyPropertyChanged
    {
        public int? ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public bool EhManual { get; set; }
        public string OrigemDescricao => EhManual ? "Manual" : "Estoque";
        public string Unidade { get; set; } = "UN";

        private decimal _quantidadeDesejada;
        public decimal QuantidadeDesejada
        {
            get => _quantidadeDesejada;
            set { _quantidadeDesejada = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QuantidadeDesejada))); }
        }

        private decimal? _quantidadeComprada;
        public decimal? QuantidadeComprada
        {
            get => _quantidadeComprada;
            set { _quantidadeComprada = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(QuantidadeComprada))); }
        }

        private bool _comprado;
        public bool Comprado
        {
            get => _comprado;
            set { _comprado = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Comprado))); }
        }

        public string? Observacoes { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
