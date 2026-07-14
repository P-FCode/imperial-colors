using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImperialColors.UI.Views;

public partial class FechamentoVendaView : Window
{
    private readonly decimal _total;
    private readonly ObservableCollection<PagamentoLinhaUi> _pagamentos = new();
    private FormaPagamento _formaSelecionada = FormaPagamento.Dinheiro;

    public FechamentoVendaResultDto? Resultado { get; private set; }

    public FechamentoVendaView(decimal total)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _total = total;
        TxtTotal.Text = FormattingHelper.FormatarMoeda(total);
        DgPagamentos.ItemsSource = _pagamentos;

        for (var i = 1; i <= 12; i++)
            CmbParcelas.Items.Add($"{i}x");
        CmbParcelas.SelectedIndex = 0;

        SelecionarForma(FormaPagamento.Dinheiro, BtnDinheiro);
        AtualizarResumo();
    }

    private void FormaPagamento_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton botao || botao.Tag is not string tag)
            return;

        if (!Enum.TryParse<FormaPagamento>(tag, out var forma))
            return;

        SelecionarForma(forma, botao);
    }

    private void SelecionarForma(FormaPagamento forma, ToggleButton botaoAtivo)
    {
        _formaSelecionada = forma;

        foreach (var child in LogicalTreeHelper.GetChildren(this)
                     .OfType<DependencyObject>()
                     .SelectMany(EnumerarFilhos)
                     .OfType<ToggleButton>()
                     .Where(b => b.Tag is string t && Enum.TryParse<FormaPagamento>(t, out _)))
        {
            child.IsChecked = ReferenceEquals(child, botaoAtivo);
        }

        PainelRecebido.Visibility = PagamentoHelper.UsaTroco(forma) ? Visibility.Visible : Visibility.Collapsed;
        PainelParcelas.Visibility = PagamentoHelper.PermiteParcelamento(forma) ? Visibility.Visible : Visibility.Collapsed;

        PreencherValorSugerido();
        AtualizarInfoTroco();
    }

    private static IEnumerable<DependencyObject> EnumerarFilhos(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in EnumerarFilhos(child))
                yield return nested;
        }
    }

    private void PreencherValorSugerido()
    {
        var saldo = CalcularSaldoRestante();
        TxtValorPagamento.Text = FormattingHelper.FormatarMoedaEntrada(saldo > 0 ? saldo : 0m);

        if (PagamentoHelper.UsaTroco(_formaSelecionada))
            TxtValorRecebido.Text = TxtValorPagamento.Text;
    }

    private void TxtValorPagamento_TextChanged(object sender, TextChangedEventArgs e) => AtualizarInfoTroco();

    private void TxtValorRecebido_TextChanged(object sender, TextChangedEventArgs e) => AtualizarInfoTroco();

    private void AtualizarInfoTroco()
    {
        if (!PagamentoHelper.UsaTroco(_formaSelecionada))
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

            var saldo = CalcularSaldoRestante();
            if (valor > saldo)
            {
                MessageBox.Show($"O valor não pode exceder o saldo restante ({FormattingHelper.FormatarMoeda(saldo)}).",
                    "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? valorRecebido = null;
            var parcelas = 1;

            if (PagamentoHelper.UsaTroco(_formaSelecionada))
            {
                if (!FormattingHelper.TryParseMoeda(TxtValorRecebido.Text, out var recebido) || recebido < valor)
                {
                    MessageBox.Show("Valor recebido em espécie insuficiente.", "Validação",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                valorRecebido = recebido;
            }
            else if (PagamentoHelper.PermiteParcelamento(_formaSelecionada))
            {
                parcelas = CmbParcelas.SelectedIndex >= 0 ? CmbParcelas.SelectedIndex + 1 : 1;
            }

            _pagamentos.Add(new PagamentoLinhaUi
            {
                Forma = _formaSelecionada,
                Valor = valor,
                ValorRecebido = valorRecebido,
                Parcelas = parcelas
            });

            AtualizarResumo();
            PreencherValorSugerido();
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
            AtualizarResumo();
            PreencherValorSugerido();
        }
    }

    private decimal CalcularSaldoRestante()
        => Math.Max(0, _total - _pagamentos.Sum(p => p.Valor));

    private void AtualizarResumo()
    {
        var totalPago = _pagamentos.Sum(p => p.Valor);
        var saldo = Math.Max(0, _total - totalPago);

        TxtTotalPago.Text = FormattingHelper.FormatarMoeda(totalPago);
        TxtSaldoRestante.Text = FormattingHelper.FormatarMoeda(saldo);
        BtnConfirmar.IsEnabled = saldo == 0 && _pagamentos.Count > 0;
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pagamentos = _pagamentos.Select(p => new CriarVendaPagamentoDto
            {
                FormaPagamento = p.Forma,
                Valor = p.Valor,
                ValorRecebido = p.ValorRecebido,
                QuantidadeParcelas = p.Parcelas
            }).ToList();

            PagamentoHelper.ValidarPagamentosCompostos(_total, pagamentos);

            Resultado = new FechamentoVendaResultDto { Pagamentos = pagamentos };
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

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
