namespace ImperialColors.Application.Configuration;

/// <summary>
/// Dados cadastrais da empresa carregados de appsettings.json / variáveis de ambiente.
/// </summary>
public class EmpresaConfig
{
    public const string Secao = "DadosEmpresa";

    public string NomeFantasia { get; set; } = "Imperial Colors";

    public string RazaoSocial { get; set; } = "Imperial Colors Tintas e Revestimentos LTDA";

    public string Subtitulo { get; set; } = "Tintas e Revestimentos";

    public string CNPJ { get; set; } = string.Empty;

    public string InscricaoEstadual { get; set; } = string.Empty;

    public string Endereco { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;
}
