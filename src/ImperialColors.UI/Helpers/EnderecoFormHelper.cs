using System.Windows.Controls;
using System.Windows.Input;

namespace ImperialColors.UI.Helpers;

public static class EnderecoFormHelper
{
    public static void AplicarMascaraUf(TextBox campoUf)
    {
        campoUf.CharacterCasing = CharacterCasing.Upper;
        campoUf.MaxLength = 2;
        campoUf.MinWidth = 64;

        campoUf.PreviewTextInput += (_, e) =>
            e.Handled = e.Text.Any(c => !char.IsLetter(c));
    }

    public static void PreencherEndereco(
        TextBox logradouro,
        TextBox bairro,
        TextBox cidade,
        TextBox uf,
        string? log,
        string? bair,
        string? cid,
        string? estado)
    {
        logradouro.Text = log ?? string.Empty;
        bairro.Text = bair ?? string.Empty;
        cidade.Text = cid ?? string.Empty;
        uf.Text = (estado ?? string.Empty).Trim().ToUpperInvariant();
    }
}
