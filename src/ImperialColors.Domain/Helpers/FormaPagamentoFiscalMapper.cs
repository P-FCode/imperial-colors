using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Traduz o <see cref="FormaPagamento"/> interno do sistema (1=Dinheiro, 2=CartaoDebito,
/// 3=CartaoCredito, 4=Pix, 5=Boleto) para o código oficial <c>tPag</c> exigido no grupo
/// <c>pag.detPag[]</c> da NF-e/NFC-e (01=Dinheiro, 03=Cartão de Crédito, 04=Cartão de
/// Débito, 15=Boleto Bancário, 17=Pix, 99=Outros) — os dois numeram na ordem que fez
/// sentido pro cadastro interno, não batem por coincidência com a tabela da SEFAZ.
/// </summary>
public static class FormaPagamentoFiscalMapper
{
    public static string ParaTPag(FormaPagamento formaPagamento) => formaPagamento switch
    {
        FormaPagamento.Dinheiro => "01",
        FormaPagamento.CartaoCredito => "03",
        FormaPagamento.CartaoDebito => "04",
        FormaPagamento.Boleto => "15",
        FormaPagamento.Pix => "17",
        _ => "99" // Outros — não deveria acontecer com o enum atual, guarda de segurança
    };
}
