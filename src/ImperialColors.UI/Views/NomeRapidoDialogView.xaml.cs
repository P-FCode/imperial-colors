using ImperialColors.UI.Helpers;
using System.Windows;
using System.Windows.Input;

namespace ImperialColors.UI.Views;

public partial class NomeRapidoDialogView : Window
{
    public string? NomeInformado { get; private set; }

    public NomeRapidoDialogView(string titulo, string labelCampo)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        TxtTitulo.Text = titulo;
        TxtLabel.Text = labelCampo;
        TxtNome.Focus();
    }

    private void BtnSalvar_Click(object sender, RoutedEventArgs e) => Confirmar();

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);

    private void TxtNome_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Confirmar();
        if (e.Key == Key.Escape)
            BtnCancelar_Click(sender, e);
    }

    private void Confirmar()
    {
        if (string.IsNullOrWhiteSpace(TxtNome.Text))
        {
            TxtErro.Text = "Informe um nome válido.";
            TxtErro.Visibility = Visibility.Visible;
            TxtNome.Focus();
            return;
        }

        NomeInformado = TxtNome.Text.Trim();
        DialogResult = true;
        Close();
    }
}
