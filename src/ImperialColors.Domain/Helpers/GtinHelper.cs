namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Confere se um código de barras é um GTIN estruturalmente válido (8, 12, 13 ou 14
/// dígitos com dígito verificador correto pelo algoritmo padrão GS1). GTIN é opcional no
/// XML da NF-e — o literal <c>"SEM GTIN"</c> é aceito em <c>cEAN</c>/<c>cEANTrib</c>
/// (GUIA_INTEGRACAO.md seção 4.2) — mas a SEFAZ rejeita a nota com "GTIN inválido" quando
/// um valor É enviado e não passa nessa conferência. Como o campo "Código de Barras" do
/// produto no cadastro de estoque nem sempre guarda um GTIN real (às vezes é um código
/// interno, PLU de balança etc.), <see cref="EhValido"/> decide se aquele valor pode ser
/// enviado como GTIN ou se deve cair para "SEM GTIN" — ver uso em
/// <c>Infrastructure.Fiscal.NotaFiscalPayloadBuilder</c>.
/// </summary>
public static class GtinHelper
{
    public static bool EhValido(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return false;

        var valor = codigo.Trim();
        if (valor.Length is not (8 or 12 or 13 or 14)) return false;
        if (!valor.All(char.IsAsciiDigit)) return false;

        var digitoVerificador = valor[^1] - '0';
        var soma = 0;
        var multiplicador = 3;
        for (var i = valor.Length - 2; i >= 0; i--)
        {
            soma += (valor[i] - '0') * multiplicador;
            multiplicador = multiplicador == 3 ? 1 : 3;
        }

        var digitoCalculado = (10 - (soma % 10)) % 10;
        return digitoCalculado == digitoVerificador;
    }
}
