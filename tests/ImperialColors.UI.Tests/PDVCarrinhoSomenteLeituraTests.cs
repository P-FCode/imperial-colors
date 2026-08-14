using System.Windows.Controls;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Services;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// O carrinho do PDV passou a ser <c>IsReadOnly="True"</c>. O motivo é que as colunas de
/// dinheiro usam <c>MoedaConverter</c>, cujo <c>ConvertBack</c> devolve <c>0</c> para
/// qualquer texto que não seja moeda válida: com o grid editável, um duplo clique em
/// "Preço Un." e uma digitação qualquer zeravam o preço da linha.
///
/// O risco é o oposto — o operador muda a quantidade dentro do próprio grid, e isso não pode
/// ter sido desligado junto. Não foi, porque a quantidade nunca dependeu do modo de edição de
/// célula: ela vive num <see cref="DataGridTemplateColumn"/>, cujo CellTemplate é renderizado
/// sempre, e <c>IsReadOnly</c> só bloqueia a ENTRADA em edição. O mesmo par já roda em
/// produção nesta mesma tela — <c>DgPagamentos</c> é somente leitura e tem um botão de
/// remover pagamento dentro de um template.
///
/// É essa estrutura que o teste protege: se alguém trocar a coluna de quantidade por uma
/// <c>DataGridTextColumn</c>, ela passa a depender do modo de edição e o <c>IsReadOnly</c>
/// deste grid deixa o operador sem como alterar quantidade no PDV.
/// </summary>
public class PDVCarrinhoSomenteLeituraTests
{
    public PDVCarrinhoSomenteLeituraTests() => WpfTestBootstrap.Inicializar();

    private static PDVView CriarPdv() => new(
        Mock.Of<IProdutoService>(),
        Mock.Of<IVendaService>(),
        Mock.Of<IClienteService>(),
        Mock.Of<IServiceProvider>(),
        Mock.Of<ISessaoService>(),
        Mock.Of<IDatabaseHealthService>(),
        Mock.Of<IContingencyVendaService>(),
        Mock.Of<IDataSyncService>());

    [StaFact]
    public void Carrinho_NaoEntraEmEdicaoDeCelulaMasMantemAQuantidadeEmTemplate()
    {
        var pdv = CriarPdv();
        var grid = (DataGrid)pdv.FindName("DgItens")!;

        Assert.True(grid.IsReadOnly, "o carrinho não pode entrar em edição de célula");

        var colunaQuantidade = grid.Columns.Single(c => (c.Header as string) == "Qtd");
        Assert.IsType<DataGridTemplateColumn>(colunaQuantidade);
        Assert.NotNull(((DataGridTemplateColumn)colunaQuantidade).CellTemplate);

        pdv.Close();
    }

    /// <summary>
    /// As colunas de dinheiro são o motivo da mudança: com o grid editável, o
    /// <c>ConvertBack</c> do MoedaConverter aceitava a edição e devolvia 0 para lixo.
    /// </summary>
    [StaFact]
    public void Carrinho_ColunasDeDinheiroNaoSaoEditaveis()
    {
        var pdv = CriarPdv();
        var grid = (DataGrid)pdv.FindName("DgItens")!;

        foreach (var titulo in new[] { "Preço Un.", "Subtotal" })
        {
            var coluna = grid.Columns.Single(c => (c.Header as string) == titulo);
            Assert.True(grid.IsReadOnly || coluna.IsReadOnly, $"coluna '{titulo}' editável");
        }

        pdv.Close();
    }
}
