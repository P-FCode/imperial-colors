namespace ImperialColors.Domain.Constants;

public static class UnidadesMedida
{
    public static readonly string[] Todas = ["UN", "GL", "LT", "RL", "CX", "PCT", "BD"];

    public static bool EhValida(string? unidade)
        => !string.IsNullOrWhiteSpace(unidade) &&
           Todas.Contains(unidade.Trim().ToUpperInvariant());

    public static string Normalizar(string? unidade, string padrao = "UN")
    {
        if (string.IsNullOrWhiteSpace(unidade))
            return padrao;

        var upper = unidade.Trim().ToUpperInvariant();
        return EhValida(upper) ? upper : padrao;
    }
}
