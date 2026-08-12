using System.Text.Json;

namespace ImperialColors.Infrastructure.Fiscal;

/// <summary>
/// Opções de serialização compartilhadas por todo o cliente da API Fiscal. Sem política de
/// nomenclatura de propriedade (<c>PropertyNamingPolicy = null</c>) — os contratos em
/// <c>Fiscal/Contracts</c> já têm o nome exato da tag/campo esperado pela API (ex.: a
/// classe declara a propriedade "ICMSDetails" e é isso que sai no JSON, sem camelCase).
/// Deserialização é case-insensitive por padrão do System.Text.Json, então a resposta da
/// API (que usa camelCase) é lida normalmente pelos DTOs de resposta.
/// </summary>
public static class FiscalJsonOptions
{
    public static readonly JsonSerializerOptions Padrao = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        // Blinda a leitura de qualquer resposta da API Fiscal contra campos que deveriam
        // ser string ("cStat": "100") virem número puro (100) — ver StringOrNumberConverter.
        Converters = { new StringOrNumberConverter() }
    };
}
