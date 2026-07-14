using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ImperialColors.Infrastructure.Services;

public class BrasilApiCnpjService
{
    private readonly HttpClient _http;

    public BrasilApiCnpjService(HttpClient http) => _http = http;

    public async Task<DadosCnpjDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digits.Length != 14)
            return null;

        var response = await _http.GetAsync($"api/cnpj/v1/{digits}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var payload = await response.Content.ReadFromJsonAsync<BrasilApiCnpjResponse>(cancellationToken: cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.RazaoSocial))
            return null;

        return new DadosCnpjDto
        {
            RazaoSocial = payload.RazaoSocial.Trim(),
            NomeFantasia = string.IsNullOrWhiteSpace(payload.NomeFantasia) ? null : payload.NomeFantasia.Trim(),
            Telefone = FormatarTelefone(payload.DddTelefone1),
            Email = string.IsNullOrWhiteSpace(payload.Email) ? null : payload.Email.Trim(),
            Cep = FormatarCep(payload.Cep),
            Logradouro = payload.Logradouro,
            Numero = payload.Numero,
            Complemento = payload.Complemento,
            Bairro = payload.Bairro,
            Cidade = payload.Municipio,
            Uf = payload.Uf
        };
    }

    private static string? FormatarTelefone(string? dddTelefone)
    {
        if (string.IsNullOrWhiteSpace(dddTelefone))
            return null;

        var digits = new string(dddTelefone.Where(char.IsDigit).ToArray());
        return digits.Length switch
        {
            10 => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
            11 => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
            _ => dddTelefone.Trim()
        };
    }

    private static string? FormatarCep(string? cep)
    {
        if (string.IsNullOrWhiteSpace(cep))
            return null;

        var digits = new string(cep.Where(char.IsDigit).ToArray());
        return digits.Length == 8 ? $"{digits[..5]}-{digits[5..]}" : cep;
    }

    private sealed class BrasilApiCnpjResponse
    {
        [JsonPropertyName("razao_social")]
        public string? RazaoSocial { get; set; }

        [JsonPropertyName("nome_fantasia")]
        public string? NomeFantasia { get; set; }

        [JsonPropertyName("ddd_telefone_1")]
        public string? DddTelefone1 { get; set; }

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
