using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Services;

public class ViaCepService : IViaCepService
{
    private readonly HttpClient _http;

    public ViaCepService(HttpClient http) => _http = http;

    public async Task<EnderecoViaCepDto?> ConsultarAsync(string cep, CancellationToken cancellationToken = default)
    {
        var digits = new string(cep.Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            return null;

        var response = await _http.GetAsync($"ws/{digits}/json/", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<ViaCepResponse>(cancellationToken: cancellationToken);
        if (payload is null || payload.Erro)
            return null;

        return new EnderecoViaCepDto
        {
            Logradouro = payload.Logradouro ?? string.Empty,
            Bairro = payload.Bairro ?? string.Empty,
            Cidade = payload.Localidade ?? string.Empty,
            Uf = payload.Uf ?? string.Empty,
            Complemento = payload.Complemento
        };
    }

    private sealed class ViaCepResponse
    {
        [JsonPropertyName("erro")]
        public bool Erro { get; set; }

        [JsonPropertyName("logradouro")]
        public string? Logradouro { get; set; }

        [JsonPropertyName("bairro")]
        public string? Bairro { get; set; }

        [JsonPropertyName("localidade")]
        public string? Localidade { get; set; }

        [JsonPropertyName("uf")]
        public string? Uf { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complemento { get; set; }
    }
}
