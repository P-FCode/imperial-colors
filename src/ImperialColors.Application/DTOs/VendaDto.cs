using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

public class VendaDto
{
    public int Id { get; set; }
    public string NumeroVenda { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public string? NomeCompradorCupom { get; set; }
    public string? DocumentoCompradorCupom { get; set; }
    public TipoPessoa? TipoPessoaComprador { get; set; }
    public string NomeCompradorExibicao => !string.IsNullOrWhiteSpace(NomeCompradorCupom)
        ? NomeCompradorCupom!
        : ClienteNome ?? "Consumidor Final";
    public string? DocumentoCompradorExibicao => DocumentoCompradorCupom;
    public StatusVenda Status { get; set; }
    public string StatusDescricao => Status switch
    {
        StatusVenda.Aberta => "Aberta",
        StatusVenda.Finalizada => "Finalizada",
        StatusVenda.Cancelada => "Cancelada",
        _ => "Desconhecido"
    };
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public decimal ValorPago { get; set; }
    public decimal Troco { get; set; }
    public List<VendaPagamentoDto> Pagamentos { get; set; } = new();
    public string FormaPagamentoDescricao => PagamentoHelper.ObterDescricaoComposta(Pagamentos, FormaPagamento, QuantidadeParcelas);
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public DateTime DataVenda { get; set; }
    public List<ItemVendaDto> Itens { get; set; } = new();
}

public class VendaPagamentoDto
{
    public int Id { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public decimal? ValorRecebido { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
    public int Ordem { get; set; }
    public string Descricao => PagamentoHelper.ObterDescricao(FormaPagamento, QuantidadeParcelas);
    public decimal Troco => PagamentoHelper.UsaTroco(FormaPagamento)
        ? Math.Max(0, (ValorRecebido ?? Valor) - Valor)
        : 0;
}

public class ItemVendaDto
{
    public int Id { get; set; }
    public int ProdutoId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? CodigoInterno { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal? PrecoOriginal { get; set; }
    public bool EmPromocao { get; set; }
    public decimal Desconto { get; set; }
    public decimal Subtotal { get; set; }
    public string Unidade { get; set; } = "UN";

    public string DescricaoTroca => string.IsNullOrWhiteSpace(NomeProduto)
        ? CodigoInterno ?? "Produto da venda"
        : NomeProduto;

    public string NomeExibicao => $"{NomeProduto}  ×{Quantidade} {Unidade}  — R$ {PrecoUnitario:N2}/un";
}

public class CriarVendaDto
{
    public int? ClienteId { get; set; }
    public bool ConsumidorFinal { get; set; } = true;
    public string? NomeCompradorAvulso { get; set; }
    public string? DocumentoCompradorAvulso { get; set; }
    public TipoPessoa? TipoPessoaCompradorAvulso { get; set; }
    public decimal Desconto { get; set; }
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Dinheiro;
    public int QuantidadeParcelas { get; set; } = 1;
    public decimal ValorPago { get; set; }
    public decimal Troco { get; set; }
    public List<CriarVendaPagamentoDto> Pagamentos { get; set; } = new();
    public string? Observacoes { get; set; }
    public string? Usuario { get; set; }
    public List<CriarItemVendaDto> Itens { get; set; } = new();
}

public class CriarVendaPagamentoDto
{
    public FormaPagamento FormaPagamento { get; set; } = FormaPagamento.Dinheiro;
    public decimal Valor { get; set; }
    public decimal? ValorRecebido { get; set; }
    public int QuantidadeParcelas { get; set; } = 1;
}

public class CriarItemVendaDto
{
    public int ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
}

public class IdentificacaoClientePdvDto
{
    public bool ConsumidorFinal { get; set; } = true;
    public int? ClienteId { get; set; }
    public string? NomeCompradorAvulso { get; set; }
    public string? DocumentoCompradorAvulso { get; set; }
    public TipoPessoa? TipoPessoaCompradorAvulso { get; set; }
}

public class FechamentoVendaResultDto
{
    public List<CriarVendaPagamentoDto> Pagamentos { get; set; } = new();
}
