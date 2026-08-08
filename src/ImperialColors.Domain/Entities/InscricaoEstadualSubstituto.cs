namespace ImperialColors.Domain.Entities;

/// <summary>
/// Inscrição Estadual de Substituto Tributário por UF — usada quando a empresa é
/// Substituta Tributária em mais de um estado (cada UF pode exigir uma IE-ST própria,
/// diferente da IE "normal" do estado sede). Lista dinâmica vinculada à configuração
/// fiscal única da empresa.
/// </summary>
public class InscricaoEstadualSubstituto
{
    public int Id { get; set; }
    public int ConfiguracaoFiscalEmpresaId { get; set; }
    public string Uf { get; set; } = string.Empty;
    public string InscricaoEstadual { get; set; } = string.Empty;

    public ConfiguracaoFiscalEmpresa ConfiguracaoFiscalEmpresa { get; set; } = null!;
}
