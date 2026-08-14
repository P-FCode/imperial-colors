using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class VendasExternasView : UserControl
{
    private readonly VendaExternaViewModel _viewModel;

    public VendasExternasView(VendaExternaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    /// <summary>
    /// Duplo clique abre a venda, como em todas as outras telas de listagem (Vendas,
    /// Clientes, Estoque, Mercadorias, Notas). Esta era a única listagem sem o handler:
    /// o duplo clique caía no comportamento padrão do DataGrid, que é entrar em edição
    /// de célula — e a coluna "Itens" é a propriedade calculada VendaExternaDto.TotalItens,
    /// somente leitura, o que estourava a associação TwoWay.
    /// </summary>
    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.VendaSelecionada is not null && _viewModel.EditarVendaCommand.CanExecute(null))
            _viewModel.EditarVendaCommand.Execute(null);
    }
}
