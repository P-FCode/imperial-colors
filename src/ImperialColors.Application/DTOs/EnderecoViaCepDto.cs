namespace ImperialColors.Application.DTOs;

public class EnderecoViaCepDto
{
    public string Logradouro { get; set; } = string.Empty;
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string? Complemento { get; set; }

    /// <summary>Código IBGE do município (7 dígitos) — usado no endereço fiscal do emitente da NF-e.</summary>
    public string? CodigoIbge { get; set; }
}
