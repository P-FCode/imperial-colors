namespace ImperialColors.Application.Configuration;

/// <summary>
/// URLs base dos microsserviços da API Fiscal externa (PFCode) que emitem NF-e/NFC-e.
/// A API Key/CSC ficam em <c>ConfiguracaoFiscalEmpresa</c> (banco, editável em runtime);
/// já a URL base fica aqui (appsettings/.env) de propósito — evita que um erro de
/// digitação no cadastro aponte a emissão para um host errado em produção.
/// </summary>
public class FiscalApiConfig
{
    public const string Secao = "FiscalApi";

    /// <summary>Base da API de NF-e (modelo 55) — <c>:5001</c> em produção.</summary>
    public string NFeBaseUrl { get; set; } = "https://fiscal.pfcode.com.br:5001";

    /// <summary>Base da API de NFC-e (modelo 65) — <c>:5002</c> em produção.</summary>
    public string NFCeBaseUrl { get; set; } = "https://fiscal.pfcode.com.br:5002";
}
