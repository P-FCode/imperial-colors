namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Mapeia a sigla da UF para o código IBGE de 2 dígitos (tag <c>cUF</c> do XML da NF-e) —
/// tabela fixa e nacional, não muda. Usado onde a API Fiscal exige o campo explicitamente
/// (ex.: inutilização de faixa, seção 8.3 do guia) — na emissão normal, <c>ide.cUF</c> é
/// opcional e a própria API resolve pela UF cadastrada no Tenant.
/// </summary>
public static class UfCodigoIbgeHelper
{
    private static readonly Dictionary<string, string> Codigos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RO"] = "11", ["AC"] = "12", ["AM"] = "13", ["RR"] = "14", ["PA"] = "15",
        ["AP"] = "16", ["TO"] = "17", ["MA"] = "21", ["PI"] = "22", ["CE"] = "23",
        ["RN"] = "24", ["PB"] = "25", ["PE"] = "26", ["AL"] = "27", ["SE"] = "28",
        ["BA"] = "29", ["MG"] = "31", ["ES"] = "32", ["RJ"] = "33", ["SP"] = "35",
        ["PR"] = "41", ["SC"] = "42", ["RS"] = "43", ["MS"] = "50", ["MT"] = "51",
        ["GO"] = "52", ["DF"] = "53"
    };

    public static string? ObterCodigo(string? uf) =>
        !string.IsNullOrWhiteSpace(uf) && Codigos.TryGetValue(uf.Trim(), out var codigo) ? codigo : null;
}
