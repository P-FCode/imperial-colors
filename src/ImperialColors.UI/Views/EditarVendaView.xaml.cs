using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImperialColors.UI.Views;

/// <summary>
/// Correção de uma venda já fechada — o caixa marcou "Dinheiro" e foi no cartão, esqueceu o
/// CPF do comprador, digitou o parcelamento errado.
///
/// Mexe só no que não altera o valor da venda: pagamento, comprador e observações. Item,
/// desconto e total ficam de fora porque mudá-los exigiria estornar e refazer a baixa de
/// estoque — para isso o caminho é a devolução (que repõe o estoque) e uma venda nova.
/// </summary>
public partial class EditarVendaView : Window
{
    private readonly IClienteService _clienteService;
    private readonly ObservableCollection<PagamentoLinhaUi> _pagamentos = new();

    private VendaDto? _venda;
    private ClienteDto? _clienteSelecionado;
    private FormaPagamento _formaSelecionada = FormaPagamento.Dinheiro;
    private bool _suprimirMascaraDocumento;
    private bool _pronto;

    /// <summary>Preenchido só quando o operador salva; null se ele fechou ou cancelou.</summary>
    public AtualizarVendaDto? Resultado { get; private set; }

    public EditarVendaView(IClienteService clienteService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _clienteService = clienteService;

        DgPagamentos.ItemsSource = _pagamentos;

        for (var i = 1; i <= 12; i++)
            CmbParcelas.Items.Add($"{i}x");
        CmbParcelas.SelectedIndex = 0;
    }

    /// <summary>
    /// Carrega a venda na tela. <paramref name="avisoNotaFiscal"/> vem preenchido quando a
    /// venda já tem nota transmitida — a nota é documento fechado e não muda junto.
    /// </summary>
    public void Inicializar(VendaDto venda, string? avisoNotaFiscal = null)
    {
        _venda = venda;

        TxtTitulo.Text = $"Editar Venda #{venda.NumeroVenda}";
        TxtTotal.Text = FormattingHelper.FormatarMoeda(venda.Total);
        TxtObservacoes.Text = venda.Observacoes ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(avisoNotaFiscal))
        {
            TxtAvisoNotaFiscal.Text = avisoNotaFiscal;
            AvisoNotaFiscal.Visibility = Visibility.Visible;
        }

        CarregarPagamentosExistentes(venda);
        CarregarCompradorExistente(venda);

        _pronto = true;
        SelecionarForma(FormaPagamento.Dinheiro, BtnDinheiro);
        AtualizarResumo();
    }

    /// <summary>
    /// A grade começa com o que está gravado para o operador corrigir uma linha só em vez de
    /// redigitar a composição inteira. Venda antiga, anterior ao pagamento composto, não tem
    /// linhas — aí o resumo do cabeçalho vira a primeira linha.
    /// </summary>
    private void CarregarPagamentosExistentes(VendaDto venda)
    {
        _pagamentos.Clear();

        if (venda.Pagamentos.Count > 0)
        {
            foreach (var pagamento in venda.Pagamentos.OrderBy(p => p.Ordem))
            {
                _pagamentos.Add(new PagamentoLinhaUi
                {
                    Forma = pagamento.FormaPagamento,
                    Valor = pagamento.Valor,
                    ValorRecebido = pagamento.ValorRecebido,
                    Parcelas = pagamento.QuantidadeParcelas
                });
            }
            return;
        }

        _pagamentos.Add(new PagamentoLinhaUi
        {
            Forma = venda.FormaPagamento,
            Valor = venda.Total,
            ValorRecebido = venda.ValorPago > 0 ? venda.ValorPago : null,
            Parcelas = Math.Max(1, venda.QuantidadeParcelas)
        });
    }

    private void CarregarCompradorExistente(VendaDto venda)
    {
        if (venda.ClienteId is > 0)
        {
            RbClienteCadastrado.IsChecked = true;
            _clienteSelecionado = new ClienteDto
            {
                Id = venda.ClienteId.Value,
                Nome = venda.ClienteNome ?? venda.NomeCompradorCupom ?? string.Empty
            };
            TxtClienteAtual.Text = $"Cliente atual: {_clienteSelecionado.Nome}";
            return;
        }

        // "Consumidor Final" é o nome que o próprio sistema grava quando ninguém foi
        // identificado — não é um comprador digitado pelo caixa.
        var identificado = !string.IsNullOrWhiteSpace(venda.NomeCompradorCupom) &&
                           !string.Equals(venda.NomeCompradorCupom, "Consumidor Final", StringComparison.OrdinalIgnoreCase);

        if (!identificado)
        {
            RbConsumidorFinal.IsChecked = true;
            return;
        }

        RbDadosCupom.IsChecked = true;
        if (venda.TipoPessoaComprador == TipoPessoa.Juridica)
            RbPessoaJuridica.IsChecked = true;
        else
            RbPessoaFisica.IsChecked = true;

        TxtNomeCupom.Text = venda.NomeCompradorCupom;
        TxtDocumentoCupom.Text = venda.DocumentoCompradorCupom ?? string.Empty;
    }

    // ---------- Comprador ----------

    private void ModoComprador_Changed(object sender, RoutedEventArgs e)
    {
        // O IsChecked disparado por Inicializar chega antes de a tela existir por inteiro.
        if (PainelClienteCadastrado is null || PainelDadosCupom is null)
            return;

        PainelClienteCadastrado.Visibility = RbClienteCadastrado.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        PainelDadosCupom.Visibility = RbDadosCupom.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void TipoPessoa_Changed(object sender, RoutedEventArgs e)
    {
        if (LblDocumentoCupom is null || LblNomeCupom is null)
            return;

        var juridica = RbPessoaJuridica.IsChecked == true;
        LblNomeCupom.Text = juridica ? "Razão Social *" : "Nome *";
        LblDocumentoCupom.Text = juridica ? "CNPJ" : "CPF";
        TxtDocumentoCupom.MaxLength = juridica ? 18 : 14;
    }

    private async void TxtBuscaCliente_TextChanged(object sender, TextChangedEventArgs e)
    {
        var termo = TxtBuscaCliente.Text.Trim();
        if (termo.Length < 2)
        {
            LstClientes.ItemsSource = null;
            return;
        }

        try
        {
            LstClientes.ItemsSource = (await _clienteService.BuscarAsync(termo)).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ExceptionMessageHelper.ObterMensagemAmigavel(ex), "Erro ao buscar clientes",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LstClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstClientes.SelectedItem is not ClienteDto cliente)
            return;

        _clienteSelecionado = cliente;
        TxtClienteAtual.Text = $"Cliente selecionado: {cliente.Nome}";
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
    }

    // ---------- Pagamento ----------

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

        foreach (var botao in new[] { BtnDinheiro, BtnPix, BtnDebito, BtnCredito, BtnBoleto })
            botao.IsChecked = ReferenceEquals(botao, botaoAtivo);

        PainelRecebido.Visibility = PagamentoHelper.UsaTroco(forma) ? Visibility.Visible : Visibility.Collapsed;
        PainelParcelas.Visibility = PagamentoHelper.PermiteParcelamento(forma) ? Visibility.Visible : Visibility.Collapsed;

        PreencherValorSugerido();
        AtualizarInfoTroco();
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
        if (TxtInfoTroco is null)
            return;

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
        if (!FormattingHelper.TryParseMoeda(TxtValorPagamento.Text, out var valor) || valor <= 0)
        {
            Avisar("Informe um valor válido para o pagamento.");
            return;
        }

        var saldo = CalcularSaldoRestante();
        if (valor > saldo)
        {
            Avisar($"O valor não pode exceder o saldo restante ({FormattingHelper.FormatarMoeda(saldo)}).");
            return;
        }

        decimal? valorRecebido = null;
        var parcelas = 1;

        if (PagamentoHelper.UsaTroco(_formaSelecionada))
        {
            if (!FormattingHelper.TryParseMoeda(TxtValorRecebido.Text, out var recebido) || recebido < valor)
            {
                Avisar("Valor recebido em espécie insuficiente.");
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

    private void BtnRemoverPagamento_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not PagamentoLinhaUi linha)
            return;

        _pagamentos.Remove(linha);
        AtualizarResumo();
        PreencherValorSugerido();
    }

    private decimal CalcularSaldoRestante()
        => Math.Max(0, (_venda?.Total ?? 0m) - _pagamentos.Sum(p => p.Valor));

    private void AtualizarResumo()
    {
        if (!_pronto)
            return;

        var totalPago = _pagamentos.Sum(p => p.Valor);
        var saldo = Math.Max(0, (_venda?.Total ?? 0m) - totalPago);

        TxtTotalPago.Text = FormattingHelper.FormatarMoeda(totalPago);
        TxtSaldoRestante.Text = FormattingHelper.FormatarMoeda(saldo);

        // Mesma trava do fechamento da venda: pagamento que não cobre o total deixaria a
        // venda com um valor pago que não bate com o que ela vale.
        BtnSalvar.IsEnabled = saldo == 0 && _pagamentos.Count > 0;
    }

    // ---------- Salvar ----------

    private void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (_venda is null)
            return;

        var pagamentos = _pagamentos.Select(p => new CriarVendaPagamentoDto
        {
            FormaPagamento = p.Forma,
            Valor = p.Valor,
            ValorRecebido = p.ValorRecebido,
            QuantidadeParcelas = p.Parcelas
        }).ToList();

        try
        {
            PagamentoHelper.ValidarPagamentosCompostos(_venda.Total, pagamentos);
        }
        catch (Exception ex)
        {
            Avisar(ex.Message);
            return;
        }

        var dto = new AtualizarVendaDto
        {
            Id = _venda.Id,
            Pagamentos = pagamentos,
            Observacoes = TxtObservacoes.Text,
            FormaPagamento = pagamentos[0].FormaPagamento,
            QuantidadeParcelas = pagamentos[0].QuantidadeParcelas,
            ValorPago = pagamentos.Sum(p => p.ValorRecebido ?? p.Valor)
        };

        if (RbClienteCadastrado.IsChecked == true)
        {
            if (_clienteSelecionado is null)
            {
                Avisar("Selecione o cliente na lista ou escolha outra forma de identificar o comprador.");
                return;
            }

            dto.ClienteId = _clienteSelecionado.Id;
            dto.ConsumidorFinal = false;
        }
        else if (RbDadosCupom.IsChecked == true)
        {
            if (string.IsNullOrWhiteSpace(TxtNomeCupom.Text))
            {
                Avisar("Informe o nome do comprador.");
                return;
            }

            dto.ConsumidorFinal = false;
            dto.NomeCompradorAvulso = TxtNomeCupom.Text.Trim();
            dto.DocumentoCompradorAvulso = string.IsNullOrWhiteSpace(TxtDocumentoCupom.Text)
                ? null
                : TxtDocumentoCupom.Text.Trim();
            dto.TipoPessoaCompradorAvulso = RbPessoaJuridica.IsChecked == true
                ? TipoPessoa.Juridica
                : TipoPessoa.Fisica;
        }
        else
        {
            dto.ConsumidorFinal = true;
        }

        Resultado = dto;
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static void Avisar(string mensagem)
        => MessageBox.Show(mensagem, "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);

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
