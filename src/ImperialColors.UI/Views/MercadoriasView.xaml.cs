using ImperialColors.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class MercadoriasView : UserControl
{
    private readonly FornecedorViewModel _fornecedorViewModel;
    private readonly ListaCompraViewModel _listaCompraViewModel;

    public MercadoriasView(FornecedorViewModel fornecedorViewModel, ListaCompraViewModel listaCompraViewModel)
    {
        InitializeComponent();
        _fornecedorViewModel = fornecedorViewModel;
        _listaCompraViewModel = listaCompraViewModel;
        PainelFornecedores.DataContext = fornecedorViewModel;
        PainelListasCompra.DataContext = listaCompraViewModel;
    }

    private void FornecedoresGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _fornecedorViewModel.ExecutarEdicaoSeSelecionado();

    private void ListasGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => _listaCompraViewModel.ExecutarEdicaoSeSelecionado();
}
