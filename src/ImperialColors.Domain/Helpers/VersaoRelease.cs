using System.Globalization;
using System.Text.RegularExpressions;

namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Traduz a tag de uma release do GitHub para uma <see cref="Version"/> comparável.
///
/// Isolado do serviço porque é aqui que mora o risco silencioso da atualização automática:
/// uma tag que não é entendida vira 0.0.0, e 0.0.0 nunca é maior que a versão instalada — o
/// efeito é o cliente clicar em "Atualizar", ser informado de que já está na versão mais
/// recente, e nunca receber a correção. Por isso a análise é tolerante com o que o autor da
/// release erra na prática (<c>v1.2.3</c>, <c>V1.2.3</c>, <c>v.1.2.3</c>, <c>1.2</c>) e
/// coberta por testes.
///
/// Vive em Domain (não em Infrastructure, onde nasceu) porque a mesma comparação de versão
/// passou a servir dois propósitos: decidir se há uma release nova no GitHub
/// (<c>AtualizadorSistemaService</c>, em Infrastructure) e registrar/comparar qual versão
/// aplicou por último as migrations no banco compartilhado
/// (<see cref="RegistroVersaoBancoHelper"/>, usado também pela Application). Duplicar a
/// lógica de parsing em duas camadas é exatamente o tipo de coisa que gera uma delas ficar
/// desatualizada silenciosamente.
/// </summary>
public static partial class VersaoRelease
{
    [GeneratedRegex(@"^[vV]\.?")]
    private static partial Regex PrefixoTag();

    /// <summary>
    /// Converte a tag em versão. Devolve <c>false</c> quando a tag não tem forma de versão —
    /// o chamador precisa distinguir isso de "versão 0.0.0", que é uma versão válida.
    /// </summary>
    public static bool TentarConverter(string? tag, out Version versao)
    {
        versao = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(tag))
            return false;

        var limpa = PrefixoTag().Replace(tag.Trim(), string.Empty);

        // Descarta sufixos de pré-lançamento e metadados (1.2.3-beta.1, 1.2.3+build).
        var corte = limpa.IndexOfAny(['-', '+', ' ']);
        if (corte >= 0)
            limpa = limpa[..corte];

        var partes = limpa.Split('.');
        if (partes.Length is 0 or > 4)
            return false;

        var numeros = new int[3];
        for (var i = 0; i < 3; i++)
        {
            if (i >= partes.Length)
                continue;
            if (!int.TryParse(partes[i], NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                return false;
            numeros[i] = n;
        }

        versao = new Version(numeros[0], numeros[1], numeros[2]);
        return true;
    }

    /// <summary>
    /// Reduz a três componentes. A versão do assembly tem quatro (1.2.3.0) e a tag tem três,
    /// então comparar direto faria 1.2.3.0 &gt; 1.2.3 e nenhuma atualização seria oferecida.
    /// </summary>
    public static Version Truncar(Version versao) =>
        new(versao.Major, versao.Minor, Math.Max(versao.Build, 0));

    public static string Formatar(Version versao) =>
        $"v{versao.Major}.{versao.Minor}.{Math.Max(versao.Build, 0)}";
}
