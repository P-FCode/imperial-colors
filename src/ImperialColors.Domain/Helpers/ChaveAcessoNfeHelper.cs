using System.Globalization;

namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Gera o <c>cNF</c> (código numérico) e monta a chave de acesso de 44 dígitos da NF-e
/// localmente, ANTES do envio — layout: <c>cUF(2)+AAMM(4)+CNPJ(14)+mod(2)+serie(3)+
/// nNF(9)+tpEmis(1)+cNF(8)+cDV(1)</c> (seção 7 do GUIA_INTEGRACAO.md).
///
/// Antes desta classe existir, o <c>cNF</c> não era enviado pelo ERP (ficava nulo no
/// payload) — a API Fiscal sorteava um valor próprio a cada chamada, então uma nota
/// reenviada com o mesmo <c>nNF</c> (ex.: retry depois de 502/504) acabava gerando uma
/// chave DIFERENTE a cada tentativa, sem o ERP nunca saber qual chave o servidor tinha
/// atribuído. Isso é exatamente o cenário do cStat 539 "Duplicidade... com diferença na
/// Chave de Acesso" (seção 9.4) e o que impedia seguir a "Regra de ouro" de consultar a
/// chave antes de reemitir em timeout (seção 9.5) — sem saber o <c>cNF</c> usado, não tinha
/// chave pra consultar. Gerando o <c>cNF</c> aqui e reconstituindo a chave completa antes
/// do POST, o ERP sempre sabe qual chave tentou usar, mesmo se a resposta nunca chegar.
/// </summary>
public static class ChaveAcessoNfeHelper
{
    public static string GerarCodigoNumerico() =>
        Random.Shared.Next(0, 100_000_000).ToString("D8", CultureInfo.InvariantCulture);

    /// <summary>Monta a chave de 44 dígitos (43 + DV), ou <c>null</c> se algum dado
    /// obrigatório estiver ausente/fora do formato — nesse caso o chamador segue sem chave
    /// tentativa, igual ao comportamento anterior a este helper existir.</summary>
    public static string? Montar(
        string? cUf, DateTime dataEmissao, string? cnpj, string modelo, string serie,
        string numero, string tpEmis, string codigoNumerico)
    {
        if (string.IsNullOrWhiteSpace(cUf) || cUf.Length != 2)
            return null;

        var cnpjDigitos = new string((cnpj ?? string.Empty).Where(char.IsDigit).ToArray());
        if (cnpjDigitos.Length != 14)
            return null;

        if (string.IsNullOrWhiteSpace(numero) || !numero.All(char.IsDigit))
            return null;

        var chave43 =
            cUf +
            dataEmissao.ToString("yyMM", CultureInfo.InvariantCulture) +
            cnpjDigitos +
            modelo.PadLeft(2, '0') +
            serie.PadLeft(3, '0') +
            numero.PadLeft(9, '0') +
            tpEmis.PadLeft(1, '0') +
            codigoNumerico.PadLeft(8, '0');

        if (chave43.Length != 43 || !chave43.All(char.IsDigit))
            return null;

        return chave43 + CalcularDigitoVerificador(chave43).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Módulo 11 da chave de acesso — pesos 2..9 da direita pra esquerda,
    /// reiniciando em 2 (diferente das tabelas de peso fixo de CPF/CNPJ).</summary>
    private static int CalcularDigitoVerificador(string chave43)
    {
        ReadOnlySpan<int> pesos = [2, 3, 4, 5, 6, 7, 8, 9];

        var soma = 0;
        var pesoIndice = 0;
        for (var i = chave43.Length - 1; i >= 0; i--)
        {
            soma += (chave43[i] - '0') * pesos[pesoIndice];
            pesoIndice = (pesoIndice + 1) % pesos.Length;
        }

        var resto = soma % 11;
        return resto is 0 or 1 ? 0 : 11 - resto;
    }
}
