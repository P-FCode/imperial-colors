namespace ImperialColors.Domain.Constants;

/// <summary>
/// Conjuntos de códigos fiscais oficiais (Manual de Orientação do Contribuinte / SEFAZ)
/// usados para validar o cadastro de tributação do produto antes de permitir salvar —
/// evita que um NCM, CST ou CSOSN digitado errado só seja descoberto na hora de emitir
/// a nota fiscal.
/// </summary>
public static class CodigosFiscais
{
    /// <summary>CST do ICMS — regime normal (Lucro Presumido/Real).</summary>
    public static readonly IReadOnlySet<string> CstIcmsValidos = new HashSet<string>
    {
        "00", "10", "20", "30", "40", "41", "50", "51", "60", "70", "90"
    };

    /// <summary>CSOSN do ICMS — Simples Nacional.</summary>
    public static readonly IReadOnlySet<string> CsosnValidos = new HashSet<string>
    {
        "101", "102", "103", "201", "202", "203", "300", "400", "500", "900"
    };

    /// <summary>CST do IPI.</summary>
    public static readonly IReadOnlySet<string> CstIpiValidos = new HashSet<string>
    {
        "00", "01", "02", "03", "04", "05", "49",
        "50", "51", "52", "53", "54", "55", "99"
    };
}
