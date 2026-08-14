using System.Windows.Controls;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Services;
using ImperialColors.UI.ViewModels;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Duplo clique numa venda externa estourava "Uma associação TwoWay ou OneWayToSource não
/// pode funcionar na propriedade somente leitura 'TotalItens'". O grid não era somente
/// leitura e era a única listagem do sistema sem handler de duplo clique, então o gesto caía
/// no comportamento padrão do DataGrid — entrar em modo de edição de célula — e a coluna
/// "Itens" é <c>VendaExternaDto.TotalItens</c>, uma propriedade calculada sem setter.
/// </summary>
public class VendasExternasGridTests
{
    public VendasExternasGridTests() => WpfTestBootstrap.Inicializar();

    private static VendasExternasView CriarTela() => new(
        new VendaExternaViewModel(
            Mock.Of<IVendaExternaService>(),
            Mock.Of<IServiceScopeFactory>(),
            Mock.Of<ISessaoService>()));

    [StaFact]
    public void GridDeVendas_NaoEntraEmEdicaoDeCelula()
    {
        var tela = CriarTela();

        var grid = (DataGrid)tela.FindName("GridVendasExternas")!;

        Assert.True(grid.IsReadOnly);
    }
}
