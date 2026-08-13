namespace ImperialColors.Application.DTOs;

/// <summary>Um resultado de busca de NCM (Nomenclatura Comum do Mercosul) — código de 8
/// dígitos (sem pontuação) e a descrição oficial da mercadoria, para o operador escolher
/// sem precisar decorar a Tabela NCM.</summary>
public class NcmSugestaoDto
{
    /// <summary>8 dígitos, sem pontos — mesmo formato exigido por
    /// <c>TributacaoProdutoValidator.ValidarNcm</c> e pelo XML da NF-e.</summary>
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
}
