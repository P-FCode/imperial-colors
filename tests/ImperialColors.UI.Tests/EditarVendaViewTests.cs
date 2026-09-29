using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Tela de correção de uma venda já fechada. Construir a janela de verdade valida o XAML
/// inteiro — StaticResource que não resolve e binding para propriedade inexistente não
/// aparecem em tempo de compilação e estourariam no primeiro clique do operador.
/// </summary>
public class EditarVendaViewTests
{
    public EditarVendaViewTests() => WpfTestBootstrap.Inicializar();

    private static EditarVendaView Criar() => new(new Mock<IClienteService>().Object);

    private static VendaDto Venda(decimal total = 100m) => new()
    {
        Id = 7,
        NumeroVenda = "000007",
        Status = StatusVenda.Finalizada,
        Total = total,
        FormaPagamento = FormaPagamento.Dinheiro,
        QuantidadeParcelas = 1,
        ValorPago = total,
        NomeCompradorCupom = "Consumidor Final"
    };

    [StaFact]
    public void Tela_ConstroiComOsCamposDeCorrecao()
    {
        var tela = Criar();

        foreach (var nome in new[]
                 {
                     "TxtTitulo", "TxtTotal", "TxtTotalPago", "TxtSaldoRestante",
                     "RbConsumidorFinal", "RbClienteCadastrado", "RbDadosCupom",
                     "TxtNomeCupom", "TxtDocumentoCupom", "DgPagamentos",
                     "TxtObservacoes", "BtnSalvar", "AvisoNotaFiscal"
                 })
        {
            Assert.True(tela.FindName(nome) is not null, $"'{nome}' não existe no XAML");
        }

        tela.Close();
    }

    /// <summary>
    /// A modal usa AplicarEstiloModerno, que deixa a janela sem borda e com fundo
    /// transparente — quem desenha a moldura é o XAML. Sem uma raiz opaca, a tela de trás
    /// (o histórico de vendas) aparece por baixo do conteúdo em vez de uma janela por cima.
    /// </summary>
    [StaFact]
    public void Tela_DesenhaAPropriaMolduraOpaca()
    {
        var tela = Criar();

        Assert.Equal(Brushes.Transparent, tela.Background);

        var moldura = Assert.IsType<Border>(tela.Content);
        var fundo = Assert.IsType<SolidColorBrush>(moldura.Background);
        Assert.Equal(byte.MaxValue, fundo.Color.A);

        tela.Close();
    }

    /// <summary>
    /// Venda antiga, de antes do pagamento composto, não tem linhas de pagamento gravadas.
    /// Se a grade abrisse vazia, o saldo apareceria como o total inteiro em aberto e o
    /// operador teria que redigitar um pagamento que já existe.
    /// </summary>
    [StaFact]
    public void VendaSemLinhasDePagamento_AbreComOResumoDoCabecalhoComoPrimeiraLinha()
    {
        var tela = Criar();

        tela.Inicializar(Venda(total: 250m));

        var grade = (DataGrid)tela.FindName("DgPagamentos")!;
        Assert.Single(grade.Items);
        Assert.True(((Button)tela.FindName("BtnSalvar")!).IsEnabled,
            "o pagamento existente cobre o total, então salvar deveria estar liberado");

        tela.Close();
    }

    /// <summary>
    /// Pagamento que não cobre o total deixaria a venda valendo 250 com 100 recebidos. O
    /// Service recusa; aqui o botão já nasce desabilitado para o operador não descobrir isso
    /// por mensagem de erro.
    /// </summary>
    [StaFact]
    public void ComSaldoEmAberto_SalvarFicaDesabilitado()
    {
        var tela = Criar();
        var venda = Venda(total: 250m);
        venda.Pagamentos.Add(new VendaPagamentoDto
        {
            FormaPagamento = FormaPagamento.Pix,
            Valor = 100m,
            QuantidadeParcelas = 1,
            Ordem = 1
        });

        tela.Inicializar(venda);

        Assert.False(((Button)tela.FindName("BtnSalvar")!).IsEnabled);

        tela.Close();
    }

    /// <summary>A nota já transmitida não muda junto com a correção — o operador precisa ler
    /// isso antes de salvar, não depois.</summary>
    [StaFact]
    public void VendaComNotaEmitida_MostraOAvisoDeQueANotaNaoMuda()
    {
        var tela = Criar();

        tela.Inicializar(Venda(), avisoNotaFiscal: "Esta venda já tem a NFC-e 1/45 (Autorizada).");

        var aviso = (Border)tela.FindName("AvisoNotaFiscal")!;
        Assert.Equal(Visibility.Visible, aviso.Visibility);

        tela.Close();
    }

    [StaFact]
    public void VendaSemNota_NaoMostraOAviso()
    {
        var tela = Criar();

        tela.Inicializar(Venda());

        Assert.Equal(Visibility.Collapsed, ((Border)tela.FindName("AvisoNotaFiscal")!).Visibility);

        tela.Close();
    }

    /// <summary>Venda que saiu como Consumidor Final tem que reabrir assim — "Consumidor
    /// Final" é o nome que o próprio sistema grava, não um comprador digitado pelo caixa.</summary>
    [StaFact]
    public void VendaDeConsumidorFinal_ReabreNaOpcaoConsumidorFinal()
    {
        var tela = Criar();

        tela.Inicializar(Venda());

        Assert.True(((RadioButton)tela.FindName("RbConsumidorFinal")!).IsChecked);

        tela.Close();
    }

    /// <summary>Comprador identificado no cupom reabre com nome e documento preenchidos: a
    /// correção costuma ser num campo só, não no cadastro inteiro.</summary>
    [StaFact]
    public void VendaComCompradorNoCupom_ReabreComOsDadosPreenchidos()
    {
        var tela = Criar();
        var venda = Venda();
        venda.NomeCompradorCupom = "Maria Souza";
        venda.DocumentoCompradorCupom = "191.191.191-00";
        venda.TipoPessoaComprador = TipoPessoa.Fisica;

        tela.Inicializar(venda);

        Assert.True(((RadioButton)tela.FindName("RbDadosCupom")!).IsChecked);
        Assert.Equal("Maria Souza", ((TextBox)tela.FindName("TxtNomeCupom")!).Text);
        Assert.Equal("191.191.191-00", ((TextBox)tela.FindName("TxtDocumentoCupom")!).Text);

        tela.Close();
    }
}
