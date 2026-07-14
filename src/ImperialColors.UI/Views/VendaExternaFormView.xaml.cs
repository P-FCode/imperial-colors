using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class VendaExternaFormView : Window
{
    private readonly IVendaExternaService _vendaExternaService;
    private readonly IProdutoService _produtoService;
    private string? _usuario;
    private int _vendaId;
    private bool _modoEdicao;
    private List<ProdutoDto> _produtosEncontrados = new();
    private readonly ObservableCollection<ItemVendaExternaFormModel> _itens = new();

    public VendaExternaFormView(
        IVendaExternaService vendaExternaService,
        IProdutoService produtoService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _vendaExternaService = vendaExternaService;
        _produtoService = produtoService;
        GridItens.ItemsSource = _itens;
        _itens.CollectionChanged += Itens_CollectionChanged;
    }

    public void InicializarNova(string? usuario)
    {
        _modoEdicao = false;
        _vendaId = 0;
        _usuario = usuario;
        Title = "Registrar Venda Externa";
        TxtTituloModulo.Text = "Registrar Venda Externa";
        TxtSubtituloModulo.Text = "Adicione itens do estoque, digite manualmente ou importe uma lista TXT para conferência";
        BtnAprovar.Content = "Aprovar e Concluir Venda";
        TxtObservacoes.Text = string.Empty;
        RbModoEstoque.IsChecked = true;
        _itens.Clear();
        LimparCamposItem();
        LimparErroValidacao();
        AtualizarTotal();
    }

    public void InicializarEdicao(VendaExternaDto venda, string? usuario)
    {
        ArgumentNullException.ThrowIfNull(venda);

        _modoEdicao = true;
        _vendaId = venda.Id;
        _usuario = usuario;
        Title = $"Editar Venda Externa — {venda.NumeroVendaExterna}";
        TxtTituloModulo.Text = "Editar Venda Externa";
        TxtSubtituloModulo.Text = $"Venda {venda.NumeroVendaExterna} — ajuste quantidades, preços ou itens. O estoque será recalculado ao salvar.";
        BtnAprovar.Content = "Salvar Alterações";
        TxtObservacoes.Text = venda.Observacoes ?? string.Empty;
        RbModoEstoque.IsChecked = true;
        _itens.Clear();

        foreach (var item in venda.Itens)
        {
            _itens.Add(new ItemVendaExternaFormModel
            {
                Id = item.Id,
                ProdutoId = item.ProdutoId,
                CodigoBarras = item.CodigoBarras,
                NomeProduto = item.NomeProduto,
                Quantidade = item.Quantidade,
                PrecoBase = item.PrecoBase,
                PrecoUnitario = item.PrecoUnitario
            });
        }

        LimparCamposItem();
        LimparErroValidacao();
        AtualizarTotal();
    }

    private void Itens_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ItemVendaExternaFormModel item in e.NewItems)
                item.PropertyChanged += Item_PropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (ItemVendaExternaFormModel item in e.OldItems)
                item.PropertyChanged -= Item_PropertyChanged;
        }

        AtualizarTotal();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemVendaExternaFormModel.Subtotal)
            or nameof(ItemVendaExternaFormModel.Quantidade)
            or nameof(ItemVendaExternaFormModel.PrecoUnitario))
            AtualizarTotal();
    }

    private void AtualizarTotal()
    {
        var total = _itens.Sum(i => i.Subtotal);
        TxtTotalVenda.Text = FormattingHelper.FormatarMoeda(total);
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
        {
            TxtBuscaProduto.Text = produto.Nome;
            TxtPrecoEstoque.Text = produto.PrecoVenda.ToString("N2");
        }
    }

    private async void LstResultadosProduto_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstResultadosProduto.SelectedItem is ProdutoDto produto)
        {
            TxtBuscaProduto.Text = produto.Nome;
            TxtPrecoEstoque.Text = produto.PrecoVenda.ToString("N2");
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

        if (!FormattingHelper.TryParseMoeda(TxtPrecoEstoque.Text, out var precoUnitario) || precoUnitario < 0)
        {
            ExibirErroValidacao("Informe o valor praticado na rua.");
            return;
        }

        _itens.Add(new ItemVendaExternaFormModel
        {
            ProdutoId = produto.Id,
            CodigoBarras = produto.CodigoBarras,
            NomeProduto = produto.Nome,
            Quantidade = quantidade,
            PrecoBase = produto.PrecoVenda,
            PrecoUnitario = precoUnitario
        });

        LimparCamposItem();
    }

    private void BtnAdicionarItemManual_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        var nome = TxtNomeManual.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            ExibirErroValidacao("Informe o nome do produto.");
            return;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidadeManual.Text, out var quantidade) || quantidade <= 0)
        {
            ExibirErroValidacao("Informe uma quantidade válida.");
            return;
        }

        if (!FormattingHelper.TryParseMoeda(TxtPrecoManual.Text, out var precoUnitario) || precoUnitario < 0)
        {
            ExibirErroValidacao("Informe o valor unitário.");
            return;
        }

        _itens.Add(new ItemVendaExternaFormModel
        {
            ProdutoId = null,
            NomeProduto = nome,
            Quantidade = quantidade,
            PrecoBase = 0,
            PrecoUnitario = precoUnitario
        });

        LimparCamposItem();
    }

    private async void BtnImportarTxt_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        var dialog = new OpenFileDialog
        {
            Filter = "Arquivo de texto (*.txt)|*.txt",
            Title = "Selecionar lista de produtos"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var conteudo = await File.ReadAllTextAsync(dialog.FileName);
            var linhas = await _vendaExternaService.ProcessarImportacaoTxtAsync(conteudo);

            foreach (var linha in linhas)
            {
                _itens.Add(new ItemVendaExternaFormModel
                {
                    ProdutoId = linha.ProdutoId,
                    CodigoBarras = linha.CodigoBarras,
                    NomeProduto = linha.NomeProduto,
                    Quantidade = linha.Quantidade,
                    PrecoBase = linha.PrecoBase,
                    PrecoUnitario = linha.PrecoUnitario
                });
            }

            MessageBox.Show(
                $"{linhas.Count} item(ns) importado(s). Revise a grade antes de concluir a venda.",
                "Importação concluída",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (DomainException ex)
        {
            ExibirErroValidacao(ex.Message);
        }
        catch (Exception ex)
        {
            ExibirErroValidacao($"Erro ao importar arquivo: {ex.Message}");
        }
    }

    private void BtnRemoverItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ItemVendaExternaFormModel item })
            return;

        _itens.Remove(item);
    }

    private void GridItens_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(AtualizarTotal));
    }

    private async void BtnAprovar_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        if (_itens.Count == 0)
        {
            ExibirErroValidacao("Adicione pelo menos um item à venda externa.");
            return;
        }

        foreach (var item in _itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
            {
                ExibirErroValidacao("Todos os itens devem ter um nome de produto.");
                return;
            }

            if (item.Quantidade <= 0)
            {
                ExibirErroValidacao($"Quantidade inválida para '{item.NomeProduto}'.");
                return;
            }

            if (item.PrecoUnitario < 0)
            {
                ExibirErroValidacao($"Valor praticado inválido para '{item.NomeProduto}'.");
                return;
            }
        }

        BtnAprovar.IsEnabled = false;
        try
        {
            if (_modoEdicao)
            {
                var dtoEdicao = new AtualizarVendaExternaDto
                {
                    Id = _vendaId,
                    Observacoes = TxtObservacoes.Text.Trim(),
                    Usuario = _usuario,
                    Itens = _itens.Select(i => new AtualizarItemVendaExternaDto
                    {
                        Id = i.Id,
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.NomeProduto,
                        CodigoBarras = i.CodigoBarras,
                        Quantidade = i.Quantidade,
                        PrecoBase = i.PrecoBase,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                };

                var vendaAtualizada = await _vendaExternaService.AtualizarAsync(dtoEdicao);

                MessageBox.Show(
                    $"Venda externa #{vendaAtualizada.NumeroVendaExterna} atualizada!\nTotal: {FormattingHelper.FormatarMoeda(vendaAtualizada.Total)}",
                    "Alterações salvas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var dto = new RegistrarVendaExternaDto
                {
                    Observacoes = TxtObservacoes.Text.Trim(),
                    Usuario = _usuario,
                    Itens = _itens.Select(i => new RegistrarItemVendaExternaDto
                    {
                        ProdutoId = i.ProdutoId,
                        NomeProduto = i.NomeProduto,
                        CodigoBarras = i.CodigoBarras,
                        Quantidade = i.Quantidade,
                        PrecoBase = i.PrecoBase,
                        PrecoUnitario = i.PrecoUnitario
                    }).ToList()
                };

                var venda = await _vendaExternaService.RegistrarAsync(dto);

                MessageBox.Show(
                    $"Venda externa #{venda.NumeroVendaExterna} registrada com sucesso!\nTotal: {FormattingHelper.FormatarMoeda(venda.Total)}",
                    "Venda concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
        catch (DomainException ex)
        {
            ExibirErroValidacao(ex.Message);
        }
        catch (Exception ex)
        {
            ExibirErroValidacao(_modoEdicao
                ? $"Erro ao salvar alterações: {ex.Message}"
                : $"Erro ao registrar venda externa: {ex.Message}");
        }
        finally
        {
            BtnAprovar.IsEnabled = true;
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LimparCamposItem()
    {
        TxtBuscaProduto.Text = string.Empty;
        TxtQuantidadeEstoque.Text = "1";
        TxtPrecoEstoque.Text = string.Empty;
        TxtNomeManual.Text = string.Empty;
        TxtQuantidadeManual.Text = "1";
        TxtPrecoManual.Text = string.Empty;
        PopupResultadosProduto.IsOpen = false;
    }

    private void ExibirErroValidacao(string mensagem)
    {
        TxtErroValidacao.Text = mensagem;
        TxtErroValidacao.Visibility = Visibility.Visible;
    }

    private void LimparErroValidacao()
    {
        TxtErroValidacao.Text = string.Empty;
        TxtErroValidacao.Visibility = Visibility.Collapsed;
    }
}
