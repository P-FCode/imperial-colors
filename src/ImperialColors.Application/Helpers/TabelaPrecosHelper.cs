namespace ImperialColors.Application.Helpers;

public static class TabelaPrecosHelper
{
    public const decimal AcrescimoTabelaPintorPercentual = 5m;

    public static decimal CalcularPrecoExibicao(decimal precoVenda, decimal acrescimoPercentual = 0m)
        => Math.Round(precoVenda * (1m + acrescimoPercentual / 100m), 2, MidpointRounding.AwayFromZero);

    public static string ObterCodigoBarrasExibicao(string? codigoBarras, string codigoInterno)
        => !string.IsNullOrWhiteSpace(codigoBarras) ? codigoBarras : codigoInterno;
}
