namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Valida CPF e CNPJ pelo algoritmo oficial de dígito verificador (módulo 11) — não é
/// só formato/tamanho. Um documento com dígito errado (ex.: dois números trocados por
/// digitação) passa despercebido até a SEFAZ rejeitar a nota fiscal na emissão; validar
/// aqui pega o erro no cadastro.
/// </summary>
public static class DocumentoFiscalHelper
{
    public static bool CpfValido(string? cpf)
    {
        var digitos = SomenteDigitos(cpf);
        if (digitos.Length != 11 || TodosDigitosIguais(digitos))
            return false;

        var numeros = ParaNumeros(digitos);

        var dv1 = CalcularDigitoVerificador(numeros[..9], [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        if (dv1 != numeros[9])
            return false;

        var dv2 = CalcularDigitoVerificador(numeros[..10], [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return dv2 == numeros[10];
    }

    public static bool CnpjValido(string? cnpj)
    {
        var digitos = SomenteDigitos(cnpj);
        if (digitos.Length != 14 || TodosDigitosIguais(digitos))
            return false;

        var numeros = ParaNumeros(digitos);

        var dv1 = CalcularDigitoVerificador(numeros[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        if (dv1 != numeros[12])
            return false;

        var dv2 = CalcularDigitoVerificador(numeros[..13], [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return dv2 == numeros[13];
    }

    private static int CalcularDigitoVerificador(int[] numeros, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++)
            soma += numeros[i] * pesos[i];

        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }

    private static string SomenteDigitos(string? valor)
        => valor is null ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());

    private static int[] ParaNumeros(string digitos)
        => digitos.Select(c => c - '0').ToArray();

    private static bool TodosDigitosIguais(string digitos)
        => digitos.Distinct().Count() == 1;
}
