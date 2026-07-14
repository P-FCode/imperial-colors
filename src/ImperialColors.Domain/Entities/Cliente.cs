using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class Cliente : BaseEntity
{
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Fisica;
    public string Nome { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Telefone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Observacoes { get; set; }

    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
