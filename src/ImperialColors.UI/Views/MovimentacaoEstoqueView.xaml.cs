using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using System.Windows;

namespace ImperialColors.UI.Views;

public partial class MovimentacaoEstoqueView : Window
{
    private readonly IProdutoService _produtoService;
    private readonly ISessaoService _sessaoService;
    private ProdutoDto? _produto;

    public MovimentacaoEstoqueView(IProdutoService produtoService, ISessaoService sessaoService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _produtoService = produtoService;
        _sessaoService = sessaoService;
    }

    public void InicializarProduto(ProdutoDto produto)
    {
        ArgumentNullException.ThrowIfNull(produto);

        _produto = produto;
        TxtNomeProduto.Text = $"{produto.CodigoInterno} - {produto.NomeExibicao}";
        TxtEstoqueAtual.Text = FormattingHelper.FormatarQuantidadeUnidade(produto.QuantidadeEstoque, produto.Unidade);
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (_produto is null) return;
        if (string.IsNullOrWhiteSpace(TxtMotivo.Text))
        {
            MessageBox.Show("Motivo é obrigatório.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out decimal quantidade) || quantidade <= 0)
        {
            MessageBox.Show("Quantidade inválida.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var tipo = (CmbTipo.SelectedIndex) switch
            {
                0 => TipoMovimentacao.Entrada,
                1 => TipoMovimentacao.Saida,
                _ => TipoMovimentacao.Ajuste
            };

            await _produtoService.RegistrarMovimentacaoAsync(new MovimentacaoEstoqueDto
            {
                ProdutoId = _produto.Id,
                Tipo = tipo,
                Quantidade = quantidade,
                Motivo = TxtMotivo.Text.Trim(),
                Usuario = _sessaoService.ObterNomeUsuario()
            });

            MessageBox.Show("Movimentação registrada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);
}
