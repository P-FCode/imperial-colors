namespace ImperialColors.Application.DTOs;

public class EnderecoViaCepDto
{
    public string Logradouro { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Complemento { get; set; }
}
