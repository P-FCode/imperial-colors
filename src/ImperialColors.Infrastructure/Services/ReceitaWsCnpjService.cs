using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Services;

public class ReceitaWsCnpjService
{
    private readonly HttpClient _http;

    public ReceitaWsCnpjService(HttpClient http) => _http = http;

    public async Task<DadosCnpjDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
            return null;

        var response = await _http.GetAsync($"v1/cnpj/{digits}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<ReceitaWsResponse>(cancellationToken: cancellationToken);
        if (payload is null || !string.Equals(payload.Status, "OK", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.IsNullOrWhiteSpace(payload.Nome))
            return null;

        return new DadosCnpjDto
        {
            RazaoSocial = payload.Nome.Trim(),
            NomeFantasia = string.IsNullOrWhiteSpace(payload.Fantasia) ? null : payload.Fantasia.Trim(),
            Telefone = string.IsNullOrWhiteSpace(payload.Telefone) ? null : payload.Telefone.Trim(),
            Email = string.IsNullOrWhiteSpace(payload.Email) ? null : payload.Email.Trim(),
            Cep = NormalizarCep(payload.Cep),
            Logradouro = payload.Logradouro,
            Numero = payload.Numero,
            Complemento = payload.Complemento,
            Bairro = payload.Bairro,
            Cidade = payload.Municipio,
            Uf = payload.Uf
        };
    }

    private static string? NormalizarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return null;

        var digits = new string(cep.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? $"{digits[..5]}-{digits[5..]}" : cep.Trim();
    }

    private sealed class ReceitaWsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("nome")]
        public string? Nome { get; set; }

        [JsonPropertyName("fantasia")]
        public string? Fantasia { get; set; }

        [JsonPropertyName("telefone")]
        public string? Telefone { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("cep")]
        public string? Cep { get; set; }

        [JsonPropertyName("logradouro")]
        public string? Logradouro { get; set; }

        [JsonPropertyName("numero")]
        public string? Numero { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complemento { get; set; }

        [JsonPropertyName("bairro")]
        public string? Bairro { get; set; }

        [JsonPropertyName("municipio")]
        public string? Municipio { get; set; }

        [JsonPropertyName("uf")]
        public string? Uf { get; set; }
    }
}
