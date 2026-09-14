using System.Windows.Controls;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Services;
using ImperialColors.UI.ViewModels;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// As duas telas do módulo Orçamento só quebram em runtime quando um StaticResource do tema
/// não resolve — o compilador aceita a referência. Construir cada tela aqui trava esse risco.
/// </summary>
public class OrcamentoViewsTests
{
    public OrcamentoViewsTests() => WpfTestBootstrap.Inicializar();

    private static OrcamentosView CriarListagem() => new(
        new OrcamentoViewModel(
            Mock.Of<IOrcamentoService>(),
            Mock.Of<IRelatorioService>(),
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ISessaoService>()));

    private static OrcamentoFormView CriarFormulario() => new(
        Mock.Of<IOrcamentoService>(),
        Mock.Of<IProdutoService>(),
        Mock.Of<IClienteService>(),
        Mock.Of<IRelatorioService>());

    [StaFact]
    public void Listagem_DeveSerSomenteLeitura()
    {
        var tela = CriarListagem();

        var grid = (DataGrid)tela.FindName("GridOrcamentos")!;

        Assert.True(grid.IsReadOnly);
    }

    [StaFact]
    public void Formulario_Novo_DeveSugerirValidadeFuturaEComecarSemItens()
    {
        var form = CriarFormulario();

        form.InicializarNovo("Teste");

        var validade = (DatePicker)form.FindName("DpValidade")!;
        var grid = (DataGrid)form.FindName("GridItens")!;

        Assert.NotNull(validade.SelectedDate);
        Assert.True(validade.SelectedDate!.Value.Date > DateTime.Today);
        Assert.Empty(grid.Items);
    }

    [StaFact]
    public void Formulario_Edicao_DeveCarregarItensDoOrcamento()
    {
        var form = CriarFormulario();

        form.InicializarEdicao(new OrcamentoDto
        {
            Id = 7,
            NumeroOrcamento = "ORC-20260101-0001",
            NomeCliente = "Cliente Teste",
            TelefoneCliente = "11999998888",
            DataOrcamento = DateTime.Today,
            DataValidade = DateTime.Today.AddDays(5),
            Desconto = 10m,
            Itens =
            [
                new ItemOrcamentoDto
                {
                    NomeProduto = "Tinta",
                    Unidade = "LT",
                    Quantidade = 2m,
                    PrecoUnitario = 50m,
                    Subtotal = 100m
                }
            ]
        }, "Teste");

        var grid = (DataGrid)form.FindName("GridItens")!;
        var nome = (TextBox)form.FindName("TxtNomeCliente")!;
        var desconto = (TextBox)form.FindName("TxtDesconto")!;

        Assert.Single(grid.Items);
        Assert.Equal("Cliente Teste", nome.Text);
        Assert.Equal(10m.ToString("N2"), desconto.Text);
    }
}
