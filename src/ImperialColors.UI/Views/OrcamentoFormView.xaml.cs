using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Models;
using ImperialColors.UI.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class OrcamentoFormView : Window
{
    /// <summary>Validade sugerida quando o orçamento é novo — o usuário pode trocar a data.</summary>
    private const int DiasValidadePadrao = 7;

    private readonly IOrcamentoService _orcamentoService;
    private readonly IProdutoService _produtoService;
    private readonly IClienteService _clienteService;
    private readonly IRelatorioService _relatorioService;

    private readonly ObservableCollection<ItemOrcamentoFormModel> _itens = new();

    private string? _usuario;
    private int _orcamentoId;
    private int? _clienteId;
    private bool _modoEdicao;
    private bool _preenchendoCliente;
    private List<ProdutoDto> _produtosEncontrados = new();

    public OrcamentoFormView(
        IOrcamentoService orcamentoService,
        IProdutoService produtoService,
        IClienteService clienteService,
        IRelatorioService relatorioService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _orcamentoService = orcamentoService;
        _produtoService = produtoService;
        _clienteService = clienteService;
        _relatorioService = relatorioService;
        GridItens.ItemsSource = _itens;
        _itens.CollectionChanged += Itens_CollectionChanged;
    }

    public void InicializarNovo(string? usuario)
    {
        _modoEdicao = false;
        _orcamentoId = 0;
        _clienteId = null;
        _usuario = usuario;

        Title = "Novo Orçamento";
        TxtTituloModulo.Text = "Novo Orçamento";
        TxtSubtituloModulo.Text = "Proposta comercial sem compromisso — não reserva estoque nem gera venda";
        BtnSalvar.Content = "Salvar Orçamento";

        PreencherCliente(string.Empty, string.Empty, null);
        DpValidade.SelectedDate = Relogio.Agora.Date.AddDays(DiasValidadePadrao);
        TxtObservacoes.Text = string.Empty;
        TxtDesconto.Text = string.Empty;
        RbModoEstoque.IsChecked = true;
        _itens.Clear();

        LimparCamposItem();
        LimparErroValidacao();
        AtualizarTotais();
    }

    public void InicializarEdicao(OrcamentoDto orcamento, string? usuario)
    {
        ArgumentNullException.ThrowIfNull(orcamento);

        _modoEdicao = true;
        _orcamentoId = orcamento.Id;
        _usuario = usuario;

        Title = $"Editar Orçamento — {orcamento.NumeroOrcamento}";
        TxtTituloModulo.Text = "Editar Orçamento";
        TxtSubtituloModulo.Text = $"Orçamento {orcamento.NumeroOrcamento} — {orcamento.StatusDescricao}";
        BtnSalvar.Content = "Salvar Alterações";

        PreencherCliente(orcamento.NomeCliente, orcamento.TelefoneCliente ?? string.Empty, orcamento.ClienteId);
        DpValidade.SelectedDate = orcamento.DataValidade.Date;
        TxtObservacoes.Text = orcamento.Observacoes ?? string.Empty;
        TxtDesconto.Text = orcamento.Desconto > 0 ? orcamento.Desconto.ToString("N2") : string.Empty;
        RbModoEstoque.IsChecked = true;

        _itens.Clear();
        foreach (var item in orcamento.Itens)
        {
            _itens.Add(new ItemOrcamentoFormModel
            {
                ProdutoId = item.ProdutoId,
                NomeProduto = item.NomeProduto,
                CodigoProduto = item.CodigoProduto,
                Unidade = item.Unidade,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario
            });
        }

        LimparCamposItem();
        LimparErroValidacao();
        AtualizarTotais();
    }

    // ----------------------------------------------------------------- Cliente

    private void PreencherCliente(string nome, string telefone, int? clienteId)
    {
        _preenchendoCliente = true;
        try
        {
            TxtNomeCliente.Text = nome;
            TxtTelefoneCliente.Text = telefone;
            _clienteId = clienteId;
            AtualizarAvisoClienteVinculado();
        }
        finally
        {
            _preenchendoCliente = false;
        }
    }

    private void AtualizarAvisoClienteVinculado()
    {
        var vinculado = _clienteId.HasValue;
        TxtClienteVinculado.Text = vinculado
            ? "Cliente vinculado ao cadastro. Editar o nome desfaz o vínculo e mantém apenas o texto digitado."
            : string.Empty;
        TxtClienteVinculado.Visibility = vinculado ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void TxtNomeCliente_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_preenchendoCliente)
            return;

        // Digitar por cima do nome significa cliente avulso: o vínculo com o cadastro cai,
        // mas o nome digitado continua valendo como snapshot do orçamento.
        _clienteId = null;
        AtualizarAvisoClienteVinculado();

        var termo = TxtNomeCliente.Text.Trim();
        if (termo.Length < 2)
        {
            PopupResultadosCliente.IsOpen = false;
            return;
        }

        try
        {
            var clientes = (await _clienteService.BuscarAsync(termo)).Take(20).ToList();
            if (clientes.Count > 0)
            {
                LstResultadosCliente.ItemsSource = clientes;
                PopupResultadosCliente.IsOpen = true;
            }
            else
            {
                PopupResultadosCliente.IsOpen = false;
            }
        }
        catch (Exception ex)
        {
            PopupResultadosCliente.IsOpen = false;
            ExibirErroValidacao($"Erro ao buscar clientes: {ex.Message}");
        }
    }

    private void LstResultadosCliente_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LstResultadosCliente.SelectedItem is not ClienteDto cliente)
            return;

        PreencherCliente(cliente.Nome, cliente.Telefone ?? string.Empty, cliente.Id);
        PopupResultadosCliente.IsOpen = false;
    }

    // ------------------------------------------------------------------- Itens

    private void Itens_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ItemOrcamentoFormModel item in e.NewItems)
                item.PropertyChanged += Item_PropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (ItemOrcamentoFormModel item in e.OldItems)
                item.PropertyChanged -= Item_PropertyChanged;
        }

        AtualizarTotais();
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ItemOrcamentoFormModel.Subtotal)
            or nameof(ItemOrcamentoFormModel.Quantidade)
            or nameof(ItemOrcamentoFormModel.PrecoUnitario))
            AtualizarTotais();
    }

    private void AtualizarTotais()
    {
        var subtotal = _itens.Sum(i => i.Subtotal);
        var desconto = LerDesconto();
        var total = Math.Max(0, subtotal - desconto);

        TxtSubtotalOrcamento.Text = FormattingHelper.FormatarMoeda(subtotal);
        TxtTotalOrcamento.Text = FormattingHelper.FormatarMoeda(total);
    }

    private decimal LerDesconto()
        => FormattingHelper.TryParseMoeda(TxtDesconto.Text, out var desconto) && desconto > 0 ? desconto : 0m;

    private void TxtDesconto_TextChanged(object sender, TextChangedEventArgs e) => AtualizarTotais();

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

        var produto = produtoSelecionado;

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
            ExibirErroValidacao("Informe o valor unitário do item.");
            return;
        }

        _itens.Add(new ItemOrcamentoFormModel
        {
            ProdutoId = produto.Id,
            NomeProduto = produto.Nome,
            CodigoProduto = produto.CodigoInterno,
            Unidade = produto.Unidade,
            Quantidade = quantidade,
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
            ExibirErroValidacao("Informe a descrição do item.");
            return;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidadeManual.Text, out var quantidade) || quantidade <= 0)
        {
            ExibirErroValidacao("Informe uma quantidade válida.");
            return;
        }

        if (!FormattingHelper.TryParseMoeda(TxtPrecoManual.Text, out var precoUnitario) || precoUnitario < 0)
        {
            ExibirErroValidacao("Informe o valor unitário do item.");
            return;
        }

        _itens.Add(new ItemOrcamentoFormModel
        {
            ProdutoId = null,
            NomeProduto = nome,
            CodigoProduto = null,
            Unidade = string.IsNullOrWhiteSpace(TxtUnidadeManual.Text) ? "UN" : TxtUnidadeManual.Text.Trim(),
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario
        });

        LimparCamposItem();
    }

    private void BtnRemoverItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ItemOrcamentoFormModel item })
            _itens.Remove(item);
    }

    private void GridItens_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        => Dispatcher.BeginInvoke(new Action(AtualizarTotais));

    // ------------------------------------------------------------------ Salvar

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
        => await SalvarAsync(gerarPdf: false);

    private async void BtnSalvarGerarPdf_Click(object sender, RoutedEventArgs e)
        => await SalvarAsync(gerarPdf: true);

    private async Task SalvarAsync(bool gerarPdf)
    {
        LimparErroValidacao();

        if (!ValidarFormulario(out var dataValidade))
            return;

        BtnSalvar.IsEnabled = false;
        BtnSalvarGerarPdf.IsEnabled = false;
        try
        {
            var orcamento = _modoEdicao
                ? await _orcamentoService.AtualizarAsync(MontarDto(new AtualizarOrcamentoDto { Id = _orcamentoId }, dataValidade))
                : await _orcamentoService.RegistrarAsync(MontarDto(new RegistrarOrcamentoDto(), dataValidade));

            if (gerarPdf && !await TentarGerarPdfAsync(orcamento))
                return;

            MessageBox.Show(
                $"Orçamento {orcamento.NumeroOrcamento} salvo!\nTotal: {FormattingHelper.FormatarMoeda(orcamento.Total)}",
                "Orçamento salvo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

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
            BtnSalvarGerarPdf.IsEnabled = true;
        }
    }

    private bool ValidarFormulario(out DateTime dataValidade)
    {
        dataValidade = default;

        if (string.IsNullOrWhiteSpace(TxtNomeCliente.Text))
        {
            ExibirErroValidacao("Informe o nome do cliente.");
            return false;
        }

        if (DpValidade.SelectedDate is not { } validade)
        {
            ExibirErroValidacao("Informe a data de validade do orçamento.");
            return false;
        }

        dataValidade = validade.Date;

        if (_itens.Count == 0)
        {
            ExibirErroValidacao("Adicione pelo menos um item ao orçamento.");
            return false;
        }

        foreach (var item in _itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
            {
                ExibirErroValidacao("Todos os itens devem ter uma descrição.");
                return false;
            }

            if (item.Quantidade <= 0)
            {
                ExibirErroValidacao($"Quantidade inválida para '{item.NomeProduto}'.");
                return false;
            }

            if (item.PrecoUnitario < 0)
            {
                ExibirErroValidacao($"Valor unitário inválido para '{item.NomeProduto}'.");
                return false;
            }
        }

        var desconto = LerDesconto();
        if (desconto > _itens.Sum(i => i.Subtotal))
        {
            ExibirErroValidacao("O desconto não pode ser maior que o subtotal dos itens.");
            return false;
        }

        return true;
    }

    private T MontarDto<T>(T dto, DateTime dataValidade) where T : RegistrarOrcamentoDto
    {
        dto.ClienteId = _clienteId;
        dto.NomeCliente = TxtNomeCliente.Text.Trim();
        dto.TelefoneCliente = string.IsNullOrWhiteSpace(TxtTelefoneCliente.Text) ? null : TxtTelefoneCliente.Text.Trim();
        dto.DataValidade = dataValidade;
        dto.Desconto = LerDesconto();
        dto.Observacoes = string.IsNullOrWhiteSpace(TxtObservacoes.Text) ? null : TxtObservacoes.Text.Trim();
        dto.Usuario = _usuario;
        dto.Itens = _itens.Select(i => new ItemOrcamentoEntradaDto
        {
            ProdutoId = i.ProdutoId,
            NomeProduto = i.NomeProduto,
            CodigoProduto = i.CodigoProduto,
            Unidade = i.Unidade,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        return dto;
    }

    /// <summary>
    /// O orçamento já está gravado quando isto roda. Cancelar o salvamento do arquivo
    /// não pode desfazer nada — só devolve o usuário ao formulário.
    /// </summary>
    private async Task<bool> TentarGerarPdfAsync(OrcamentoDto orcamento)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Orcamento_{orcamento.NumeroOrcamento}",
            DefaultExt = ".pdf",
            Filter = "PDF|*.pdf"
        };

        if (dialog.ShowDialog() != true)
        {
            ExibirErroValidacao(
                $"Orçamento {orcamento.NumeroOrcamento} foi salvo, mas o PDF não foi gerado. " +
                "Use o botão 'Gerar PDF' na listagem quando quiser o arquivo.");
            return false;
        }

        await _relatorioService.GerarOrcamentoPdfAsync(orcamento, dialog.FileName);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = dialog.FileName,
            UseShellExecute = true
        });

        return true;
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
        TxtUnidadeManual.Text = "UN";
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
