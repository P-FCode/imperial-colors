using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class ProdutoFormView : Window
{
    private readonly IProdutoService _produtoService;
    private readonly ICategoriaService _categoriaService;
    private readonly IMarcaService _marcaService;
    private readonly IFornecedorService _fornecedorService;
    private int? _produtoId;
    private bool _codigoDefinidoManualmente;
    private bool _codigoDesbloqueadoManualmente;
    private string? _ultimoCodigoGeradoAutomaticamente;
    private bool _ignorarAlteracaoCodigoInterno;
    private bool _modoCustoTotal;
    private bool _suprimirAtualizacaoCusto;
    private bool _suprimirEventosUi;

    public ProdutoFormView(
        IProdutoService produtoService,
        ICategoriaService categoriaService,
        IMarcaService marcaService,
        IFornecedorService fornecedorService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _produtoService = produtoService;
        _categoriaService = categoriaService;
        _marcaService = marcaService;
        _fornecedorService = fornecedorService;

        SelecionarUnidadePadrao();
        Loaded += OnLoadedInicial;
    }

    private async void OnLoadedInicial(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedInicial;
        await CarregarComboBoxesAsync();
    }

    private void SelecionarUnidadePadrao()
    {
        foreach (ComboBoxItem item in CmbUnidade.Items)
        {
            if (item.Content?.ToString() == "UN")
            {
                CmbUnidade.SelectedItem = item;
                break;
            }
        }
    }

    private async Task CarregarComboBoxesAsync(int? categoriaId = null, int? marcaId = null, int? fornecedorId = null)
    {
        var categorias = (await _categoriaService.ObterTodosAsync()).ToList();
        var marcas = (await _marcaService.ObterTodosAsync()).ToList();
        var fornecedores = (await _fornecedorService.ObterTodosAsync()).ToList();

        CmbCategoria.ItemsSource = categorias;
        CmbMarca.ItemsSource = marcas;
        CmbFornecedor.ItemsSource = fornecedores;

        if (categoriaId.HasValue && categoriaId > 0)
            CmbCategoria.SelectedValue = categoriaId;
        else if (categorias.Count > 0)
            CmbCategoria.SelectedIndex = 0;

        if (marcaId.HasValue && marcaId > 0)
            CmbMarca.SelectedValue = marcaId;
        else if (marcas.Count > 0)
            CmbMarca.SelectedIndex = 0;

        CmbFornecedor.SelectedValue = fornecedorId is > 0 ? fornecedorId : null;
    }

    public void InicializarNovo()
    {
        TxtTitulo.Text = "Novo Produto";
        _produtoId = null;
        _codigoDefinidoManualmente = false;
        _codigoDesbloqueadoManualmente = false;
        _ultimoCodigoGeradoAutomaticamente = null;
        LimparErroValidacao();
        ChkCustoTotal.IsChecked = false;
        TxtCustoTotal.Text = string.Empty;
        PainelCustoTotal.Visibility = Visibility.Collapsed;
        TxtCusto.IsReadOnly = false;
        TxtCusto.Text = string.Empty;
        TxtPrecoVenda.Text = FormattingHelper.FormatarMoedaEntrada(0m);
        TxtNome.Text = string.Empty;
        TxtCodigoBarras.Text = string.Empty;
        TxtObservacoes.Text = string.Empty;
        DpValidade.SelectedDate = null;
        ChkPromocaoAtiva.IsChecked = false;
        TxtPrecoPromocional.Text = string.Empty;
        PainelPrecoPromocional.Visibility = Visibility.Collapsed;
        DefinirCodigoInternoSemMarcarManual(string.Empty);
        AplicarEstadoCampoCodigo();
    }

    public void InicializarEdicao(ProdutoDto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        _suprimirEventosUi = true;
        try
        {
            TxtTitulo.Text = "Editar Produto";
            _produtoId = produto.Id;
            _codigoDefinidoManualmente = true;
            _codigoDesbloqueadoManualmente = false;
            LimparErroValidacao();
            ChkCustoTotal.IsChecked = false;
            TxtCustoTotal.Text = string.Empty;
            PainelCustoTotal.Visibility = Visibility.Collapsed;
            TxtCusto.IsReadOnly = false;
            DefinirCodigoInternoSemMarcarManual(produto.CodigoInterno ?? string.Empty);
            TxtCodigoBarras.Text = produto.CodigoBarras ?? string.Empty;
            TxtNome.Text = produto.Nome ?? string.Empty;
            TxtQuantidade.Text = produto.QuantidadeEstoque.ToString(
                produto.QuantidadeEstoque % 1m == 0m ? "N0" : "N1",
                FormattingHelper.CulturaPtBr);
            TxtEstoqueMinimo.Text = produto.EstoqueMinimo.ToString(
                produto.EstoqueMinimo % 1m == 0m ? "N0" : "N1",
                FormattingHelper.CulturaPtBr);
            TxtCusto.Text = FormattingHelper.FormatarMoedaEntrada(produto.Custo);
            TxtPrecoVenda.Text = FormattingHelper.FormatarMoedaEntrada(produto.PrecoVenda);
            TxtObservacoes.Text = produto.Observacoes ?? string.Empty;
            DpValidade.SelectedDate = produto.DataValidade;
            ChkPromocaoAtiva.IsChecked = produto.PromocaoAtiva;
            TxtPrecoPromocional.Text = FormattingHelper.FormatarMoedaEntrada(produto.PrecoPromocional);
            PainelPrecoPromocional.Visibility = produto.PromocaoAtiva ? Visibility.Visible : Visibility.Collapsed;

            foreach (ComboBoxItem item in CmbUnidade.Items)
            {
                if (item.Content?.ToString() == produto.Unidade)
                {
                    CmbUnidade.SelectedItem = item;
                    break;
                }
            }

            AtualizarVisibilidadeLitragemGl();
            if (produto.LitragemGl.HasValue)
                SelecionarLitragemGl(produto.LitragemGl.Value);
        }
        finally
        {
            _suprimirEventosUi = false;
        }

        AplicarEstadoCampoCodigo();
        _ = CarregarComboBoxesAsync(produto.CategoriaId, produto.MarcaId, produto.FornecedorId);
    }

    private async void TxtNome_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_produtoId.HasValue || _codigoDefinidoManualmente || _codigoDesbloqueadoManualmente)
            return;

        var nome = TxtNome.Text.Trim();
        if (string.IsNullOrWhiteSpace(nome))
            return;

        await GerarCodigoPorNomeAsync(nome);
    }

    private async Task GerarCodigoPorNomeAsync(string nome)
    {
        var codigo = await _produtoService.GerarCodigoInternoPorNomeAsync(nome);
        _codigoDefinidoManualmente = false;
        DefinirCodigoInternoSemMarcarManual(codigo);
        _ultimoCodigoGeradoAutomaticamente = codigo;
    }

    private void BtnAlternarCodigoManual_Click(object sender, RoutedEventArgs e)
    {
        _codigoDesbloqueadoManualmente = !_codigoDesbloqueadoManualmente;
        if (!_codigoDesbloqueadoManualmente && !_produtoId.HasValue)
            _codigoDefinidoManualmente = false;

        AplicarEstadoCampoCodigo();
    }

    private void AplicarEstadoCampoCodigo()
    {
        var editavel = _codigoDesbloqueadoManualmente;
        TxtCodigoInterno.IsReadOnly = !editavel;
        BtnAlternarCodigoManual.Content = editavel ? "🔓" : "🔒";
        BtnAlternarCodigoManual.ToolTip = editavel
            ? "Bloquear e voltar à geração automática pelo nome"
            : "Desbloquear edição manual do código";
    }

    private void DefinirCodigoInternoSemMarcarManual(string codigo)
    {
        _ignorarAlteracaoCodigoInterno = true;
        TxtCodigoInterno.Text = codigo;
        _ignorarAlteracaoCodigoInterno = false;
    }

    private void TxtCodigoInterno_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ignorarAlteracaoCodigoInterno || _produtoId.HasValue || !_codigoDesbloqueadoManualmente)
            return;

        var textoAtual = TxtCodigoInterno.Text.Trim();
        _codigoDefinidoManualmente = !string.Equals(
            textoAtual,
            _ultimoCodigoGeradoAutomaticamente,
            StringComparison.OrdinalIgnoreCase);
    }

    private void ChkPromocaoAtiva_Changed(object sender, RoutedEventArgs e)
    {
        if (PainelPrecoPromocional is null || ChkPromocaoAtiva is null)
            return;

        PainelPrecoPromocional.Visibility = ChkPromocaoAtiva.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void CmbUnidade_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suprimirEventosUi) return;
        AtualizarVisibilidadeLitragemGl();
    }

    private void AtualizarVisibilidadeLitragemGl()
    {
        if (PainelLitragemGl is null || CmbUnidade is null) return;

        var unidade = (CmbUnidade.SelectedItem as ComboBoxItem)?.Content?.ToString();
        PainelLitragemGl.Visibility = unidade == "GL" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelecionarLitragemGl(decimal litragem)
    {
        if (CmbLitragemGl is null) return;

        foreach (ComboBoxItem item in CmbLitragemGl.Items)
        {
            if (item.Tag is string tag &&
                decimal.TryParse(tag, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) &&
                val == litragem)
            {
                CmbLitragemGl.SelectedItem = item;
                return;
            }
        }
    }

    private decimal? ObterLitragemGlSelecionada()
    {
        var unidade = (CmbUnidade.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (unidade != "GL") return null;
        var tag = (CmbLitragemGl.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        return decimal.TryParse(tag, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) ? val : null;
    }

    private void ChkCustoTotal_Changed(object sender, RoutedEventArgs e)
    {
        _modoCustoTotal = ChkCustoTotal.IsChecked == true;
        PainelCustoTotal.Visibility = _modoCustoTotal ? Visibility.Visible : Visibility.Collapsed;
        TxtCusto.IsReadOnly = _modoCustoTotal;

        if (_modoCustoTotal)
            CalcularCustoUnitarioPeloTotal();
        else
            TxtCustoTotal.Text = string.Empty;
    }

    private void TxtQuantidade_TextChanged(object sender, TextChangedEventArgs e)
        => CalcularCustoUnitarioPeloTotal();

    private void TxtCustoTotal_TextChanged(object sender, TextChangedEventArgs e)
        => CalcularCustoUnitarioPeloTotal();

    private void TxtCusto_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirAtualizacaoCusto || _modoCustoTotal)
            return;
    }

    private void CalcularCustoUnitarioPeloTotal()
    {
        if (!_modoCustoTotal)
            return;

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out var quantidade) || quantidade <= 0)
        {
            DefinirCustoCalculado(null);
            return;
        }

        if (!FormattingHelper.TryParseMoeda(TxtCustoTotal.Text, out var total) || total <= 0)
        {
            DefinirCustoCalculado(null);
            return;
        }

        DefinirCustoCalculado(total / quantidade);
    }

    private void DefinirCustoCalculado(decimal? custoUnitario)
    {
        _suprimirAtualizacaoCusto = true;
        TxtCusto.Text = custoUnitario.HasValue
            ? FormattingHelper.FormatarMoedaEntrada(custoUnitario.Value)
            : string.Empty;
        _suprimirAtualizacaoCusto = false;
    }

    private async void BtnNovaCategoria_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NomeRapidoDialogView("Nova Categoria", "Nome da Categoria *");
        if (ModalWindowHelper.ExibirDialogo(dialog, this) != true || string.IsNullOrWhiteSpace(dialog.NomeInformado))
            return;

        try
        {
            var criada = await _categoriaService.CriarAsync(dialog.NomeInformado);
            await CarregarComboBoxesAsync(criada.Id, ObterIdSelecionado(CmbMarca), ObterIdSelecionado(CmbFornecedor));
            LimparErroValidacao();
        }
        catch (Exception ex)
        {
            ExibirErroValidacao(ExceptionMessageHelper.ObterMensagemAmigavel(ex));
        }
    }

    private async void BtnNovaMarca_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NomeRapidoDialogView("Nova Marca", "Nome da Marca *");
        if (ModalWindowHelper.ExibirDialogo(dialog, this) != true || string.IsNullOrWhiteSpace(dialog.NomeInformado))
            return;

        try
        {
            var criada = await _marcaService.CriarAsync(dialog.NomeInformado);
            await CarregarComboBoxesAsync(ObterIdSelecionado(CmbCategoria), criada.Id, ObterIdSelecionado(CmbFornecedor));
            LimparErroValidacao();
        }
        catch (Exception ex)
        {
            ExibirErroValidacao(ExceptionMessageHelper.ObterMensagemAmigavel(ex));
        }
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        LimparErroValidacao();

        if (!ValidarFormulario())
            return;

        if (!await ValidarCodigoBarrasDuplicadoAsync())
            return;

        try
        {
            FormattingHelper.TryParseMoedaOpcional(TxtCusto.Text, out decimal? custo);
            FormattingHelper.TryParseMoeda(TxtPrecoVenda.Text, out decimal preco);
            FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out decimal quantidade);
            FormattingHelper.TryParseQuantidade(TxtEstoqueMinimo.Text, out decimal estoqueMin);

            var promocaoAtiva = ChkPromocaoAtiva.IsChecked == true;
            decimal? precoPromocional = null;
            if (promocaoAtiva)
            {
                if (!FormattingHelper.TryParseMoeda(TxtPrecoPromocional.Text, out var promo) || promo <= 0)
                {
                    ExibirErroValidacao("Informe o preço promocional.");
                    return;
                }

                precoPromocional = promo;
            }

            var categoriaId = ObterIdSelecionado(CmbCategoria);
            var marcaId = ObterIdSelecionado(CmbMarca);
            var fornecedorId = ObterIdSelecionado(CmbFornecedor);
            var unidade = (CmbUnidade.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "UN";
            var litragemGl = ObterLitragemGlSelecionada();

            BtnSalvar.IsEnabled = false;
            BtnSalvar.Content = "Salvando...";

            if (_produtoId.HasValue)
            {
                var dto = new AtualizarProdutoDto
                {
                    Id = _produtoId.Value,
                    CodigoInterno = TxtCodigoInterno.Text.Trim(),
                    CodigoBarras = string.IsNullOrWhiteSpace(TxtCodigoBarras.Text) ? null : TxtCodigoBarras.Text.Trim(),
                    Nome = TxtNome.Text.Trim(),
                    CategoriaId = categoriaId,
                    MarcaId = marcaId,
                    FornecedorId = fornecedorId,
                    DataValidade = DpValidade.SelectedDate,
                    QuantidadeEstoque = quantidade,
                    EstoqueMinimo = estoqueMin,
                    Unidade = unidade,
                    LitragemGl = litragemGl,
                    Custo = custo,
                    PrecoVenda = preco,
                    PromocaoAtiva = promocaoAtiva,
                    PrecoPromocional = precoPromocional,
                    Observacoes = string.IsNullOrWhiteSpace(TxtObservacoes.Text) ? null : TxtObservacoes.Text.Trim()
                };
                await _produtoService.AtualizarAsync(_produtoId.Value, dto);
            }
            else
            {
                var dto = new CriarProdutoDto
                {
                    CodigoInterno = TxtCodigoInterno.Text.Trim(),
                    CodigoInternoDefinidoManualmente = _codigoDefinidoManualmente,
                    CodigoBarras = string.IsNullOrWhiteSpace(TxtCodigoBarras.Text) ? null : TxtCodigoBarras.Text.Trim(),
                    Nome = TxtNome.Text.Trim(),
                    CategoriaId = categoriaId,
                    MarcaId = marcaId,
                    FornecedorId = fornecedorId,
                    DataValidade = DpValidade.SelectedDate,
                    QuantidadeEstoque = quantidade,
                    EstoqueMinimo = estoqueMin,
                    Unidade = unidade,
                    LitragemGl = litragemGl,
                    Custo = custo,
                    PrecoVenda = preco,
                    PromocaoAtiva = promocaoAtiva,
                    PrecoPromocional = precoPromocional,
                    Observacoes = string.IsNullOrWhiteSpace(TxtObservacoes.Text) ? null : TxtObservacoes.Text.Trim()
                };
                await _produtoService.CriarAsync(dto);
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
            ExibirErroValidacao(ExceptionMessageHelper.ObterMensagemAmigavel(ex));
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar Produto";
        }
    }

    private bool ValidarFormulario()
    {
        if (string.IsNullOrWhiteSpace(TxtNome.Text))
        {
            ExibirErroValidacao("Nome do produto é obrigatório.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TxtCodigoInterno.Text))
        {
            ExibirErroValidacao("Código do produto é obrigatório. Preencha o nome para gerar automaticamente ou desbloqueie o campo.");
            return false;
        }

        var categoriaId = ObterIdSelecionado(CmbCategoria);
        var marcaId = ObterIdSelecionado(CmbMarca);
        if (!categoriaId.HasValue || !marcaId.HasValue)
        {
            ExibirErroValidacao("Selecione uma Categoria e uma Marca válidas.");
            return false;
        }

        var unidadeValidacao = (CmbUnidade.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (unidadeValidacao == "GL" && ObterLitragemGlSelecionada() is null)
        {
            ExibirErroValidacao("Selecione a litragem do Galão (3,6L ou 18L).");
            return false;
        }

        if (!FormattingHelper.TryParseMoedaOpcional(TxtCusto.Text, out decimal? custo))
        {
            ExibirErroValidacao("Preço de custo inválido.");
            return false;
        }

        if (_modoCustoTotal)
        {
            if (!FormattingHelper.TryParseMoeda(TxtCustoTotal.Text, out var totalCompra) || totalCompra <= 0)
            {
                ExibirErroValidacao("Informe o valor total da compra.");
                return false;
            }

            if (!FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out var quantidadeCompra) || quantidadeCompra <= 0)
            {
                ExibirErroValidacao("Informe a quantidade em estoque para calcular o custo unitário.");
                return false;
            }

            CalcularCustoUnitarioPeloTotal();
            FormattingHelper.TryParseMoedaOpcional(TxtCusto.Text, out custo);
        }

        if (custo is <= 0 && (_modoCustoTotal || !string.IsNullOrWhiteSpace(TxtCusto.Text)))
        {
            ExibirErroValidacao("Preço de custo, quando informado, deve ser maior que zero.");
            return false;
        }

        if (!FormattingHelper.TryParseMoeda(TxtPrecoVenda.Text, out decimal preco) || preco <= 0)
        {
            ExibirErroValidacao("Preço de venda deve ser maior que zero.");
            return false;
        }

        if (ChkPromocaoAtiva.IsChecked == true)
        {
            if (!FormattingHelper.TryParseMoeda(TxtPrecoPromocional.Text, out var promo) || promo <= 0)
            {
                ExibirErroValidacao("Informe o preço promocional.");
                return false;
            }

            if (promo > preco)
            {
                ExibirErroValidacao("O preço promocional não pode ser maior que o preço de venda padrão.");
                return false;
            }
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out decimal quantidade) || quantidade < 0)
        {
            ExibirErroValidacao("Quantidade em estoque não pode ser negativa.");
            return false;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtEstoqueMinimo.Text, out decimal estoqueMin) || estoqueMin < 0)
        {
            ExibirErroValidacao("Estoque mínimo não pode ser negativo.");
            return false;
        }

        return true;
    }

    private async void TxtCodigoBarras_LostFocus(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtCodigoBarras.Text))
            return;

        await ValidarCodigoBarrasDuplicadoAsync();
    }

    private async Task<bool> ValidarCodigoBarrasDuplicadoAsync()
    {
        var codigo = TxtCodigoBarras.Text.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return true;

        var existe = await _produtoService.CodigoBarrasExisteAsync(codigo, _produtoId);
        if (!existe)
            return true;

        ExibirErroValidacao(ProdutoService.MensagemCodigoBarrasDuplicado);
        TxtCodigoBarras.Focus();
        return false;
    }

    private static int? ObterIdSelecionado(ComboBox combo)
    {
        return combo.SelectedValue switch
        {
            int id when id > 0 => id,
            long id when id > 0 => (int)id,
            _ when combo.SelectedItem is CategoriaDto c && c.Id > 0 => c.Id,
            _ when combo.SelectedItem is MarcaDto m && m.Id > 0 => m.Id,
            _ when combo.SelectedItem is FornecedorDto f && f.Id > 0 => f.Id,
            _ => null
        };
    }

    private void ExibirErroValidacao(string mensagem)
    {
        TxtErroValidacao.Text = mensagem;
        TxtErroValidacao.Visibility = Visibility.Visible;
        MessageBox.Show(mensagem, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void LimparErroValidacao()
    {
        TxtErroValidacao.Text = string.Empty;
        TxtErroValidacao.Visibility = Visibility.Collapsed;
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
