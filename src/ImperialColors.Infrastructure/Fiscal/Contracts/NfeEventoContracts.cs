using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Fiscal.Contracts;

/// <summary>Seção 8.1 do guia — prazo: 24h (NF-e) / 30min (NFC-e) da autorização.</summary>
public class CancelamentoRequest
{
    [JsonPropertyName("chaveAcesso")] public string ChaveAcesso { get; set; } = string.Empty;
    [JsonPropertyName("cnpj")] public string Cnpj { get; set; } = string.Empty;

    /// <summary>Protocolo devolvido na EMISSÃO — não é a chave de acesso.</summary>
    [JsonPropertyName("nProt")] public string NProt { get; set; } = string.Empty;

    /// <summary>Mínimo 15 caracteres.</summary>
    [JsonPropertyName("justificativa")] public string Justificativa { get; set; } = string.Empty;
    [JsonPropertyName("tpAmb")] public string? TpAmb { get; set; }
}

/// <summary>Seção 8.2 do guia — só NF-e, prazo 720h, máx. 20 por nota. Bloqueado
/// localmente pela API se o texto contiver VALOR/DESTINATARIO/IMPOSTO/PRECO.</summary>
public class CartaCorrecaoRequest
{
    [JsonPropertyName("chaveAcesso")] public string ChaveAcesso { get; set; } = string.Empty;
    [JsonPropertyName("cnpj")] public string Cnpj { get; set; } = string.Empty;

    /// <summary>Mínimo 15 caracteres.</summary>
    [JsonPropertyName("correcao")] public string Correcao { get; set; } = string.Empty;
    [JsonPropertyName("sequencial")] public int Sequencial { get; set; } = 1;
    [JsonPropertyName("tpAmb")] public string? TpAmb { get; set; }
}

/// <summary>Seção 8.3 do guia — só NF-e, faixa de numeração, sem chave/protocolo vinculável.</summary>
public class InutilizacaoRequest
{
    [JsonPropertyName("cnpj")] public string Cnpj { get; set; } = string.Empty;
    [JsonPropertyName("cUF")] public string CUF { get; set; } = string.Empty;
    [JsonPropertyName("ano")] public string Ano { get; set; } = string.Empty;
    [JsonPropertyName("modelo")] public string Modelo { get; set; } = "55";
    [JsonPropertyName("serie")] public string Serie { get; set; } = string.Empty;
    [JsonPropertyName("numeroInicial")] public string NumeroInicial { get; set; } = string.Empty;
    [JsonPropertyName("numeroFinal")] public string NumeroFinal { get; set; } = string.Empty;

    /// <summary>Mínimo 15 caracteres.</summary>
    [JsonPropertyName("justificativa")] public string Justificativa { get; set; } = string.Empty;
    [JsonPropertyName("tpAmb")] public string? TpAmb { get; set; }
}
