namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Mesma regra de arredondamento no PDV, no servidor e na contingência: o banco grava valores
/// com 2 casas e quantidades com 3, então a tela precisa cobrar exatamente o que será gravado.
/// </summary>
public static class ArredondamentoHelper
{
    public static decimal Centavos(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    public static decimal Quantidade(decimal valor) => Math.Round(valor, 3, MidpointRounding.AwayFromZero);
}
