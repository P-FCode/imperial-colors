using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ImperialColors.Application.Security;

public static partial class InputSanitizer
{
    private static readonly Regex CaracteresDeControle = ControleRegex();
    private static readonly Regex CaracteresPerigosos = PerigososRegex();

    /// <summary>
    /// Normaliza texto livre de cadastro: remove caracteres de controle (que corrompem a
    /// impressão do cupom e o XML da NF-e), apara as pontas e corta no limite da coluna.
    ///
    /// Deliberadamente NÃO remove <c>&lt; &gt; &amp; " '</c>. Numa loja de tintas esses
    /// caracteres são dado legítimo — medida em polegadas (<c>Trincha 2"</c>), razão social
    /// (<c>Silva &amp; Filhos Tintas LTDA</c>) e sobrenome (<c>Maria D'Ávila</c>) — e o mesmo
    /// texto sai impresso no cupom e vai para a NF-e. Não existe superfície HTML neste app
    /// (WPF, sem WebBrowser), todo acesso ao banco é parametrizado pelo EF Core e o payload
    /// fiscal é serializado por System.Text.Json, que já escapa o que precisa. Escapar é
    /// responsabilidade de quem renderiza; filtrar na entrada só apagava o dado do cliente.
    /// </summary>
    public static string SanitizarTexto(string? valor, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var limpo = CaracteresDeControle.Replace(valor, string.Empty).Trim();

        return limpo.Length > maxLength ? limpo[..maxLength] : limpo;
    }

    public static string SanitizarUsername(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var limpo = valor.Trim().ToLowerInvariant();
        limpo = Regex.Replace(limpo, @"[^a-z0-9._-]", string.Empty);
        return limpo.Length > 50 ? limpo[..50] : limpo;
    }

    /// <summary>
    /// E-mail mantém o filtro restritivo: <c>&lt; &gt; &amp; " '</c> não aparecem nos endereços
    /// que <see cref="EmailValido"/> aceita, e tirar os sinais de um
    /// <c>&lt;maria@loja.com&gt;</c> colado é o comportamento desejado.
    /// </summary>
    public static string SanitizarEmail(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var limpo = valor.Trim().ToLowerInvariant();
        limpo = CaracteresPerigosos.Replace(limpo, string.Empty);
        return limpo.Length > 254 ? limpo[..254] : limpo;
    }

    public static bool EmailValido(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            _ = new MailAddress(email);
            return email.Contains('@') && email.Contains('.');
        }
        catch
        {
            return false;
        }
    }

    public static bool SenhaForte(string senha)
        => !string.IsNullOrWhiteSpace(senha)
           && senha.Length >= 8
           && senha.Any(char.IsUpper)
           && senha.Any(char.IsLower)
           && senha.Any(char.IsDigit);

    [GeneratedRegex(@"[<>&""']")]
    private static partial Regex PerigososRegex();

    /// <summary>C0, C1 e DEL, preservando tabulação e quebra de linha das observações.</summary>
    [GeneratedRegex(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F-\u009F]")]
    private static partial Regex ControleRegex();
}
