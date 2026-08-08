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

    /// <summary>Código IBGE do município (7 dígitos) — obrigatório em enderDest da NF-e
    /// (a SEFAZ não aceita "cidade em texto livre"). Preenchido junto com a busca de CEP.</summary>
    public string? CodigoMunicipioIbge { get; set; }

    /// <summary>indIEDest da NF-e — se o cliente é contribuinte de ICMS, isento ou não
    /// contribuinte. Obrigatório em NF-e (não em NFC-e, onde o dest é opcional).</summary>
    public IndicadorIeDestinatario? IndicadorIe { get; set; }

    public string? Observacoes { get; set; }

    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
