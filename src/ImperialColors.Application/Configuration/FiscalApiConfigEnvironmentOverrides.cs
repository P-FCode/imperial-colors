namespace ImperialColors.Application.Configuration;

/// <summary>
/// Permite sobrescrever as URLs da API Fiscal via .env sem recompilar (prioridade sobre
/// appsettings.json) — uso típico: apontar para <c>http://localhost:5001/5002</c> durante
/// testes locais em homologação, sem tocar no default de produção.
/// </summary>
public static class FiscalApiConfigEnvironmentOverrides
{
    public static void Aplicar(FiscalApiConfig config)
    {
        AplicarSeDefinido("FISCAL_API_NFE_URL", v => config.NFeBaseUrl = v);
        AplicarSeDefinido("FISCAL_API_NFCE_URL", v => config.NFCeBaseUrl = v);
    }

    private static void AplicarSeDefinido(string chave, Action<string> aplicar)
    {
        var valor = Environment.GetEnvironmentVariable(chave)?.Trim();
        if (!string.IsNullOrEmpty(valor))
            aplicar(valor);
    }
}
