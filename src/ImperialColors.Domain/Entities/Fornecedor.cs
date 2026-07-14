using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class Fornecedor : BaseEntity
{
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Juridica;
    public string Nome { get; set; } = string.Empty;
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

    public ICollection<ListaCompra> ListasCompra { get; set; } = new List<ListaCompra>();
    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();
}
