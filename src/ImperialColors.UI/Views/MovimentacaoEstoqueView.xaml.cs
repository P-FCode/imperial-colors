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

    private void CmbTipo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (TxtLabelQuantidade is null) return;

        // "Ajuste" define o novo total absoluto do estoque (ex.: contagem física de
        // inventário) — diferente de Entrada/Saída, que somam/subtraem do total atual.
        // O rótulo deixa isso explícito para não ser confundido com uma quantidade a somar.
        TxtLabelQuantidade.Text = CmbTipo.SelectedIndex == 2
            ? "Nova Quantidade Total em Estoque *"
            : "Quantidade a Movimentar *";
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (_produto is null) return;
        if (string.IsNullOrWhiteSpace(TxtMotivo.Text))
        {
            MessageBox.Show("Motivo é obrigatório.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tipo = (CmbTipo.SelectedIndex) switch
        {
            0 => TipoMovimentacao.Entrada,
            1 => TipoMovimentacao.Saida,
            _ => TipoMovimentacao.Ajuste
        };

        // Ajuste pode zerar o estoque (ex.: perda total / descontinuado) — só
        // Entrada/Saída exigem uma quantidade estritamente positiva.
        var quantidadeValida = FormattingHelper.TryParseQuantidade(TxtQuantidade.Text, out decimal quantidade)
            && (tipo == TipoMovimentacao.Ajuste ? quantidade >= 0 : quantidade > 0);

        if (!quantidadeValida)
        {
            MessageBox.Show("Quantidade inválida.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
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
