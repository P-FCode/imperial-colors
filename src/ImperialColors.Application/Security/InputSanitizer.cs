using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ImperialColors.Application.Security;

public static partial class InputSanitizer
{
    private static readonly Regex CaracteresPerigosos = PerigososRegex();

    public static string SanitizarTexto(string? valor, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        var limpo = valor.Trim();
        limpo = CaracteresPerigosos.Replace(limpo, string.Empty);
        limpo = limpo.Replace('\0', ' ');

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
}
