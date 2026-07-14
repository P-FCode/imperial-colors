using ImperialColors.Application.Helpers;

namespace ImperialColors.Application.DTOs;

public class ProdutoDto
{
    public int Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public string? CategoriaNome { get; set; }
    public int? MarcaId { get; set; }
    public string? MarcaNome { get; set; }
    public decimal QuantidadeEstoque { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal? LitragemGl { get; set; }
    public decimal? Custo { get; set; }
    public decimal PrecoVenda { get; set; }
    public bool PromocaoAtiva { get; set; }
    public decimal? PrecoPromocional { get; set; }
    public DateTime? DataValidade { get; set; }
    public int? FornecedorId { get; set; }
    public string? FornecedorNome { get; set; }
    public string? Observacoes { get; set; }

    public bool EstoqueBaixo => QuantidadeEstoque <= EstoqueMinimo && QuantidadeEstoque > 0;
    public bool SemEstoque => QuantidadeEstoque <= 0;
    public bool EmPromocao => ProdutoPrecoHelper.EstaEmPromocao(PromocaoAtiva, PrecoPromocional, PrecoVenda);
    public decimal PrecoEfetivo => ProdutoPrecoHelper.ObterPrecoEfetivo(PrecoVenda, PromocaoAtiva, PrecoPromocional);

    /// <summary>Nome para exibição com litragem (ex: "Tinta Coral - GL 18L")</summary>
    public string NomeExibicao => Unidade == "GL" && LitragemGl.HasValue
        ? $"{Nome} (GL {LitragemGl:G}L)"
        : Nome;
}

public class CriarProdutoDto
{
    public string CodigoInterno { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public int? MarcaId { get; set; }
    public decimal QuantidadeEstoque { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public string Unidade { get; set; } = "UN";
    public decimal? LitragemGl { get; set; }
    public decimal? Custo { get; set; }
    public decimal PrecoVenda { get; set; }
    public bool PromocaoAtiva { get; set; }
    public decimal? PrecoPromocional { get; set; }
    public DateTime? DataValidade { get; set; }
    public int? FornecedorId { get; set; }
    public string? Observacoes { get; set; }
    public bool CodigoInternoDefinidoManualmente { get; set; }
}

public class AtualizarProdutoDto : CriarProdutoDto
{
    public int Id { get; set; }
}
