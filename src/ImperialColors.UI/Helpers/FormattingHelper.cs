using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace ImperialColors.UI.Helpers;

public static class FormattingHelper
{
    private static bool _culturaAplicacaoConfigurada;

    public static CultureInfo CulturaPtBr { get; } = CultureInfo.GetCultureInfo("pt-BR");

    public static void ConfigurarCulturaAplicacao()
    {
        ConfigurarCulturaThread();

        if (_culturaAplicacaoConfigurada)
            return;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("pt-BR")));

        _culturaAplicacaoConfigurada = true;
    }

    public static void ConfigurarCulturaThread()
    {
        Thread.CurrentThread.CurrentCulture = CulturaPtBr;
        Thread.CurrentThread.CurrentUICulture = CulturaPtBr;
        CultureInfo.DefaultThreadCurrentCulture = CulturaPtBr;
        CultureInfo.DefaultThreadCurrentUICulture = CulturaPtBr;
    }

    public static string FormatarMoeda(decimal valor)
        => valor.ToString("C2", CulturaPtBr);

    public static string FormatarMoedaEntrada(decimal valor)
        => valor.ToString("C2", CulturaPtBr);

    public static string FormatarMoedaEntrada(decimal? valor)
        => valor.HasValue ? FormatarMoedaEntrada(valor.Value) : string.Empty;

    public static string FormatarQuantidade(decimal quantidade)
        => quantidade.ToString(quantidade % 1m == 0m ? "N0" : "N1", CulturaPtBr);

    public static string FormatarData(DateTime data)
        => data.ToString("dd/MM/yyyy", CulturaPtBr);

    public static string FormatarDataHora(DateTime data)
        => data.ToString("dd/MM/yyyy HH:mm", CulturaPtBr);

    public static string FormatarQuantidadeUnidade(decimal quantidade, string? unidade)
    {
        var sigla = string.IsNullOrWhiteSpace(unidade) ? "UN" : unidade.Trim().ToUpperInvariant();
        var nomeUnidade = ObterNomeUnidade(sigla, quantidade);
        var quantidadeFormatada = quantidade.ToString(
            quantidade % 1m == 0m ? "N0" : "N1",
            CulturaPtBr);
        return $"{quantidadeFormatada} {nomeUnidade}";
    }

    public static bool TryParseMoeda(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var normalizado = texto
            .Replace("R$", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return decimal.TryParse(normalizado, NumberStyles.Number, CulturaPtBr, out valor)
            || decimal.TryParse(normalizado.Replace(".", "").Replace(",", "."),
                NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    public static bool TryParseMoedaOpcional(string? texto, out decimal? valor)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            valor = null;
            return true;
        }

        if (TryParseMoeda(texto, out var parsed))
        {
            valor = parsed;
            return true;
        }

        valor = null;
        return false;
    }

    public static bool TryParseQuantidade(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        return decimal.TryParse(texto.Trim(), NumberStyles.Number, CulturaPtBr, out valor)
            || decimal.TryParse(texto.Replace(",", "."), NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private static string ObterNomeUnidade(string sigla, decimal quantidade)
    {
        var plural = Math.Abs(quantidade) != 1m;

        return sigla switch
        {
            "UN" => plural ? "Unidades" : "Unidade",
            "KG" => "Kg",
            "LT" => plural ? "Litros" : "Litro",
            "MT" => plural ? "Metros" : "Metro",
            "CX" => plural ? "Caixas" : "Caixa",
            "PCT" => plural ? "Pacotes" : "Pacote",
            "PC" => plural ? "Peças" : "Peça",
            "GL" => plural ? "Galões" : "Galão",
            "RL" => plural ? "Rolos" : "Rolo",
            _ => sigla
        };
    }
}
