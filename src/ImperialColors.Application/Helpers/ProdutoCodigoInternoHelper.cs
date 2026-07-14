namespace ImperialColors.Application.Helpers;

public static class ProdutoCodigoInternoHelper
{
    public const string Prefixo = "P";

    public static string FormatarSequencia(int sequencia) => $"{Prefixo}{sequencia:D5}";

    public static bool EhCodigoSequencialPadrao(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6 || codigo[0] != Prefixo[0])
            return false;

        return int.TryParse(codigo[1..], out var numero) && numero > 0;
    }

    public static bool TryExtrairSequencia(string? codigo, out int sequencia)
    {
        sequencia = 0;
        if (!EhCodigoSequencialPadrao(codigo))
            return false;

        return int.TryParse(codigo![1..], out sequencia);
    }
}
