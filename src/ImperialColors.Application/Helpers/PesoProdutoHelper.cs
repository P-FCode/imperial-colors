using System.Globalization;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Conversão e exibição do peso do produto, guardado em gramas
/// (<see cref="Domain.Entities.Produto.PesoGramas"/>).
///
/// Gramas é a unidade de armazenamento porque o operador digita um inteiro, sem vírgula
/// para errar; quilos é a unidade de leitura — é como o peso aparece na balança, na carga
/// e no peso bruto/líquido da NF-e. A conversão mora aqui para as duas não se perderem
/// pelo caminho.
/// </summary>
public static class PesoProdutoHelper
{
    /// <summary>Cultura fixa em vez de <c>CurrentCulture</c>: "5,5 kg" com vírgula decimal
    /// faz parte do texto que o operador lê, e o app só existe em pt-BR. Depender da
    /// cultura do processo faria a mesma chamada devolver "5.5 kg" num ambiente sem a
    /// configuração de thread da UI aplicada — em teste automatizado, por exemplo.</summary>
    private static readonly CultureInfo CulturaPtBr = new("pt-BR");

    public const int GramasPorQuilo = 1000;

    /// <summary>Teto de sanidade: 1 tonelada por unidade. Não existe item de tinta ou
    /// acessório que pese mais que isso — um valor acima é o preço ou o código de barras
    /// digitado no campo errado, e é melhor barrar na hora do que carregar um peso absurdo
    /// para dentro da nota fiscal.</summary>
    public const int PesoMaximoGramas = 1_000_000;

    /// <summary>Peso em quilos, como a NF-e espera (tags <c>pesoB</c>/<c>pesoL</c>).</summary>
    public static decimal? EmQuilos(int? gramas)
        => gramas.HasValue ? gramas.Value / (decimal)GramasPorQuilo : null;

    /// <summary>
    /// Peso para leitura humana: abaixo de um quilo fica em gramas ("800 g"), daí para
    /// cima vira quilos ("5,5 kg"). Mostrar "0,8 kg" para um pote de 800 g só faz o
    /// operador conferir duas vezes se digitou certo.
    /// </summary>
    public static string Formatar(int? gramas)
    {
        if (gramas is not > 0)
            return string.Empty;

        if (gramas.Value < GramasPorQuilo)
            return $"{gramas.Value} g";

        var quilos = EmQuilos(gramas)!.Value;
        return $"{quilos.ToString("0.###", CulturaPtBr)} kg";
    }
}
