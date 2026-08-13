using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;

namespace ImperialColors.Infrastructure.Services;

/// <summary>
/// Busca de NCM via BrasilAPI (mesmo provedor já usado para CNPJ neste projeto —
/// <see cref="BrasilApiCnpjService"/>) — espelha a Tabela NCM oficial (Res. Camex), sem
/// custo e sem chave de API. <c>GET /api/ncm/v1?search={termo}</c> casa o termo tanto contra
/// o código quanto contra a descrição, então o mesmo texto digitado serve para buscar por
/// dígitos ("3208") ou por palavra-chave ("tinta").
/// </summary>
public class BrasilApiNcmService : INcmService
{
    /// <summary>Cota generosa o bastante pra qualquer termo específico ("3208.10.10", "tinta
    /// acrílica"), mas evita mandar centenas de linhas pra UI num termo genérico demais
    /// ("de", "para") — o operador refina a busca digitando mais.</summary>
    private const int LimiteResultados = 40;

    private readonly HttpClient _http;

    public BrasilApiNcmService(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<NcmSugestaoDto>> BuscarAsync(string termo, CancellationToken cancellationToken = default)
    {
        var termoLimpo = termo?.Trim() ?? string.Empty;
        if (termoLimpo.Length < 2)
            return [];

        List<BrasilApiNcmResponse>? payload;
        try
        {
            var response = await _http.GetAsync($"api/ncm/v1?search={Uri.EscapeDataString(termoLimpo)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            payload = await response.Content.ReadFromJsonAsync<List<BrasilApiNcmResponse>>(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw; // debounce cancelando a busca anterior — não é falha, repropaga normalmente
        }
        catch
        {
            // Sem internet, BrasilAPI fora do ar, timeout etc. — é uma busca auxiliar, não
            // pode travar o cadastro; o operador sempre pode digitar o NCM manualmente.
            return [];
        }

        if (payload is null)
            return [];

        var hoje = DateOnly.FromDateTime(DateTime.Today);

        return payload
            // A Tabela NCM tem entradas em vários níveis de agregação (capítulo "32", posição
            // "32.08", subposição "3208.10", item "3208.20.1"...) além do código de 8 dígitos
            // que efetivamente vai na nota — só o nível folha (8 dígitos, sem pontuação) serve.
            .Select(p => new { Digitos = new string((p.Codigo ?? string.Empty).Where(char.IsDigit).ToArray()), p.Descricao, p.DataFim })
            .Where(p => p.Digitos.Length == 8 && !string.IsNullOrWhiteSpace(p.Descricao) && NcmVigente(p.DataFim, hoje))
            .Select(p => new NcmSugestaoDto { Codigo = p.Digitos, Descricao = p.Descricao!.Trim() })
            .DistinctBy(s => s.Codigo)
            .Take(LimiteResultados)
            .ToList();
    }

    /// <summary>"9999-12-31" é o valor usado pela BrasilAPI pra "sem data de fim" (código
    /// ainda vigente) — qualquer outra data no passado é um código já revogado, que não faz
    /// sentido sugerir pra uma nota emitida hoje.</summary>
    private static bool NcmVigente(string? dataFim, DateOnly hoje)
        => string.IsNullOrWhiteSpace(dataFim) || !DateOnly.TryParse(dataFim, out var data) || data >= hoje;

    private sealed class BrasilApiNcmResponse
    {
        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("descricao")]
        public string? Descricao { get; set; }

        [JsonPropertyName("data_fim")]
        public string? DataFim { get; set; }
    }
}
