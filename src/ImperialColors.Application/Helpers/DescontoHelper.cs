namespace ImperialColors.Application.Helpers;

public enum TipoDescontoVenda
{
    Valor,
    Percentual
}

public static class DescontoHelper
{
    public const decimal PercentualMaximo = 100m;

    public static decimal CalcularDescontoEmReais(decimal subtotal, decimal valorInformado, TipoDescontoVenda tipo)
    {
        if (subtotal <= 0 || valorInformado < 0)
            return 0m;

        return tipo switch
        {
            TipoDescontoVenda.Percentual => CalcularDescontoPercentual(subtotal, valorInformado),
            _ => Math.Min(valorInformado, subtotal)
        };
    }

    public static decimal CalcularDescontoPercentual(decimal subtotal, decimal percentual)
    {
        percentual = Math.Clamp(percentual, 0m, PercentualMaximo);
        return Math.Round(subtotal * percentual / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CalcularTotalLiquido(decimal subtotal, decimal descontoReais)
        => Math.Max(0m, subtotal - descontoReais);

    public static bool PercentualValido(decimal percentual)
        => percentual >= 0m && percentual <= PercentualMaximo;
}
