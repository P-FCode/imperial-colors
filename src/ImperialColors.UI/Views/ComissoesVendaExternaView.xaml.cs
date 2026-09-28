using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

/// <summary>
/// Controle dos acertos de comissão das vendas externas: o que a loja ainda deve a quem
/// vendeu na rua e o que já foi pago.
///
/// A lista só traz venda COM comissão — quem vendeu sem comissão não tem acerto pendente, e
/// deixar essas vendas aqui encheria a tela de linhas que nunca vão ser marcadas.
/// </summary>
public partial class ComissoesVendaExternaView : Window
{
    /// <summary>Mesmo tamanho de página das demais listagens do sistema.</summary>
    private const int ItensPorPagina = 50;

    private readonly IVendaExternaService _vendaExternaService;
    private bool _pronto;
    private int _paginaAtual = 1;
    private int _totalPaginas;

    public ComissoesVendaExternaView(IVendaExternaService vendaExternaService)
    {
        InitializeComponent();
        _vendaExternaService = vendaExternaService;

        Loaded += async (_, _) =>
        {
            _pronto = true;
            await CarregarAsync();
        };
    }

    private FiltroComissaoVendaExterna FiltroSelecionado => true switch
    {
        _ when RbPagas.IsChecked == true => FiltroComissaoVendaExterna.Pagas,
        _ when RbTodas.IsChecked == true => FiltroComissaoVendaExterna.Todas,
        _ => FiltroComissaoVendaExterna.APagar
    };

    private async Task CarregarAsync()
    {
        try
        {
            var filtro = FiltroSelecionado;
            var pagina = await _vendaExternaService.ObterComissoesPaginadoAsync(filtro, _paginaAtual, ItensPorPagina);
            var resumo = await _vendaExternaService.ObterResumoComissoesAsync();

            _totalPaginas = pagina.TotalPaginas;

            // Marcar o último pendente de uma página vazia essa página quando o filtro é
            // "A pagar" — volta para a última válida em vez de mostrar a grade vazia.
            if (_totalPaginas > 0 && _paginaAtual > _totalPaginas)
            {
                _paginaAtual = _totalPaginas;
                await CarregarAsync();
                return;
            }

            var comissoes = pagina.Itens;
            GridComissoes.ItemsSource = comissoes;

            TxtTotalAPagar.Text = FormattingHelper.FormatarMoeda(resumo.TotalAPagar);
            TxtQuantidadeAPagar.Text = DescreverQuantidade(resumo.QuantidadeAPagar);
            TxtTotalPago.Text = FormattingHelper.FormatarMoeda(resumo.TotalPago);
            TxtQuantidadePaga.Text = DescreverQuantidade(resumo.QuantidadePaga);
            TxtTotalDoMes.Text = FormattingHelper.FormatarMoeda(resumo.TotalDoMes);

            TxtResumoLista.Text = pagina.TotalItens == 0
                ? string.Empty
                : $"Página {_paginaAtual} de {Math.Max(_totalPaginas, 1)} — {pagina.TotalItens} venda(s) neste filtro";

            BtnPaginaAnterior.IsEnabled = _paginaAtual > 1;
            BtnPaginaProxima.IsEnabled = _paginaAtual < _totalPaginas;

            AtualizarMensagemDeListaVazia(filtro, comissoes.Count);
            AtualizarBotoes();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao carregar comissões",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string DescreverQuantidade(int quantidade)
        => quantidade == 1 ? "1 venda" : $"{quantidade} vendas";

    /// <summary>Lista vazia quer dizer coisas diferentes em cada filtro — "nada a pagar" é
    /// uma boa notícia, "nenhuma comissão registrada" é sinal de que ninguém preencheu o
    /// campo na venda.</summary>
    private void AtualizarMensagemDeListaVazia(FiltroComissaoVendaExterna filtro, int quantidade)
    {
        if (quantidade > 0)
        {
            TxtSemComissoes.Visibility = Visibility.Collapsed;
            return;
        }

        TxtSemComissoes.Text = filtro switch
        {
            FiltroComissaoVendaExterna.APagar => "Nenhuma comissão a pagar. Tudo acertado.",
            FiltroComissaoVendaExterna.Pagas => "Nenhuma comissão foi marcada como paga ainda.",
            _ => "Nenhuma venda externa com comissão registrada. A comissão é preenchida no cadastro da venda."
        };
        TxtSemComissoes.Visibility = Visibility.Visible;
    }

    private ComissaoVendaExternaDto? Selecionada => GridComissoes.SelectedItem as ComissaoVendaExternaDto;

    private void AtualizarBotoes()
    {
        var selecionada = Selecionada;
        BtnMarcarPaga.IsEnabled = selecionada is { Paga: false };
        BtnDesmarcar.IsEnabled = selecionada is { Paga: true };
    }

    private void GridComissoes_SelectionChanged(object sender, SelectionChangedEventArgs e) => AtualizarBotoes();

    private async void Filtro_Changed(object sender, RoutedEventArgs e)
    {
        // O IsChecked do XAML dispara este evento antes de a tela existir por inteiro.
        if (!_pronto) return;

        // Trocar de filtro volta para a primeira página: a página 3 de "A pagar" não tem
        // relação nenhuma com a página 3 de "Pagas".
        _paginaAtual = 1;
        await CarregarAsync();
    }

    private async void BtnPaginaAnterior_Click(object sender, RoutedEventArgs e)
    {
        if (_paginaAtual <= 1) return;
        _paginaAtual--;
        await CarregarAsync();
    }

    private async void BtnPaginaProxima_Click(object sender, RoutedEventArgs e)
    {
        if (_paginaAtual >= _totalPaginas) return;
        _paginaAtual++;
        await CarregarAsync();
    }

    private async void BtnAtualizar_Click(object sender, RoutedEventArgs e) => await CarregarAsync();

    private async void BtnMarcarPaga_Click(object sender, RoutedEventArgs e)
    {
        if (Selecionada is not { } comissao) return;

        if (MessageBox.Show(
                $"Marcar como paga a comissão de {FormattingHelper.FormatarMoeda(comissao.Comissao)} " +
                $"da venda {comissao.NumeroVendaExterna}?",
                "Confirmar pagamento", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        await AlterarSituacaoAsync(comissao.VendaExternaId, paga: true);
    }

    private async void BtnDesmarcar_Click(object sender, RoutedEventArgs e)
    {
        if (Selecionada is not { } comissao) return;

        if (MessageBox.Show(
                $"Devolver para 'a pagar' a comissão da venda {comissao.NumeroVendaExterna}?\n\n" +
                "Use isto se o acerto foi marcado por engano — a data do pagamento anterior é apagada.",
                "Desfazer pagamento", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        await AlterarSituacaoAsync(comissao.VendaExternaId, paga: false);
    }

    private async Task AlterarSituacaoAsync(int vendaExternaId, bool paga)
    {
        try
        {
            await _vendaExternaService.MarcarComissaoAsync(vendaExternaId, paga);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao atualizar a comissão",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();
}
