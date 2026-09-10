namespace ImperialColors.Domain.Constants;

public static class UnidadesMedida
{
    /// <summary>
    /// BA (balde) e LA (lata) foram adicionadas na importação da tabela de cadastro da
    /// Imperial Colors — antes disso só existiam BD (bombona) para embalagens grandes de
    /// tinta. As três coexistem de propósito: são recipientes fisicamente diferentes que o
    /// fornecedor já distingue no próprio catálogo (ex.: "balde 25kg" vs "bombona 18L" vs
    /// "lata 18L"), e o litro/kg exato de cada embalagem fica no nome do produto, não aqui —
    /// esta lista é só o tipo de recipiente, não o tamanho.
    /// </summary>
    public static readonly string[] Todas = ["UN", "GL", "LT", "RL", "CX", "PCT", "BD", "BA", "LA"];

    public static bool EhValida(string? unidade)
        => !string.IsNullOrWhiteSpace(unidade) &&
           Todas.Contains(unidade.Trim().ToUpperInvariant());

    public static string Normalizar(string? unidade, string padrao = "UN")
    {
        if (string.IsNullOrWhiteSpace(unidade))
            return padrao;

        var upper = unidade.Trim().ToUpperInvariant();
        return EhValida(upper) ? upper : padrao;
    }
}
