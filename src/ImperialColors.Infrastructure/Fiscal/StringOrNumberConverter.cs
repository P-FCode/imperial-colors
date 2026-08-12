using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Fiscal;

/// <summary>
/// Lê uma propriedade <c>string</c>/<c>string?</c> aceitando tanto <c>"100"</c> quanto
/// <c>100</c> no JSON. Os contratos em <c>Fiscal/Contracts</c> declaram campos como
/// <c>cStat</c>, <c>nProt</c>, <c>tpAmb</c> e <c>nSeqEvento</c> como string (é o formato
/// documentado no GUIA_INTEGRACAO.md, ex.: <c>"cStat": "100"</c>), mas a API Fiscal já foi
/// observada devolvendo algum desses campos como número JSON puro em vez de string — o que
/// faz o <see cref="System.Text.Json.JsonSerializer"/> padrão explodir com
/// "Cannot get the value of a token type 'Number' as a string." em vez de simplesmente ler
/// o valor. Registrado globalmente em <see cref="FiscalJsonOptions.Padrao"/> para blindar
/// toda resposta da API Fiscal contra essa inconsistência, sem precisar decorar campo por
/// campo (nem prever qual deles virá torto da próxima vez).
/// </summary>
public sealed class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var inteiro)
                ? inteiro.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Não foi possível converter o token '{reader.TokenType}' em texto.")
        };

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(value);
    }
}
