using ImperialColors.Domain.Enums;
using ImperialColors.UI.ViewModels;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class NaturezaOperacaoView : UserControl
{
    private readonly NaturezaOperacaoViewModel _viewModel;

    public NaturezaOperacaoView(NaturezaOperacaoViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        CmbTipoOperacao.ItemsSource = Enum.GetValues<TipoOperacaoFiscal>();
        CmbFinalidade.ItemsSource = Enum.GetValues<FinalidadeNfe>();

        CmbDifal.Items.Add(new ComboBoxItem { Content = "(usar configuração da empresa)", Tag = null });
        CmbDifal.Items.Add(new ComboBoxItem { Content = "Sim, aplicar DIFAL", Tag = true });
        CmbDifal.Items.Add(new ComboBoxItem { Content = "Não aplicar DIFAL", Tag = false });

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(NaturezaOperacaoViewModel.TipoOperacao))
                CmbTipoOperacao.SelectedItem = _viewModel.TipoOperacao;
            if (e.PropertyName is nameof(NaturezaOperacaoViewModel.Finalidade))
                CmbFinalidade.SelectedItem = _viewModel.Finalidade;
            if (e.PropertyName is nameof(NaturezaOperacaoViewModel.DifalNaoContribuinte))
                CmbDifal.SelectedIndex = _viewModel.DifalNaoContribuinte switch
                {
                    null => 0,
                    true => 1,
                    false => 2
                };
            // "+ Nova Natureza" chama Novo(), que sempre desliga o ModoEdicao — dar foco
            // no campo Descrição garante feedback visível mesmo quando o formulário já
            // estava vazio (nesse caso os campos não mudam de valor, então nenhum outro
            // binding dispara e o clique parecia não fazer nada).
            if (e.PropertyName is nameof(NaturezaOperacaoViewModel.ModoEdicao) && !_viewModel.ModoEdicao)
                TxtDescricao.Focus();
        };

        CmbTipoOperacao.SelectedItem = _viewModel.TipoOperacao;
        CmbFinalidade.SelectedItem = _viewModel.Finalidade;
        CmbDifal.SelectedIndex = 0;

        Loaded += async (_, _) => await _viewModel.CarregarAsync();
    }

    private void CmbTipoOperacao_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTipoOperacao.SelectedItem is TipoOperacaoFiscal t)
            _viewModel.TipoOperacao = t;
    }

    private void CmbFinalidade_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFinalidade.SelectedItem is FinalidadeNfe f)
            _viewModel.Finalidade = f;
    }

    private void CmbDifal_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbDifal.SelectedItem is ComboBoxItem item)
            _viewModel.DifalNaoContribuinte = item.Tag as bool?;
    }
}
