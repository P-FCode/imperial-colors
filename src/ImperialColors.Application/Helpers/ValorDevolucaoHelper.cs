using ImperialColors.Domain.Helpers;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Quanto o cliente realmente pagou por unidade de um item: o subtotal do item (já sem o desconto
/// do item) menos a parte proporcional do desconto geral da venda. É o valor creditado na troca.
/// </summary>
public static class ValorDevolucaoHelper
{
    public static decimal ValorUnitarioPago(
        decimal precoUnitario,
        decimal descontoItem,
        decimal subtotalItem,
        decimal quantidadeItem,
        decimal subtotalVenda,
        decimal descontoVenda)
    {
        // Sem desconto nenhum, o preço de tabela já é o valor pago — evita arredondar à toa.
        if (descontoItem == 0m && descontoVenda == 0m)
            return precoUnitario;

        if (quantidadeItem <= 0m)
            return 0m;

        var rateioDescontoVenda = subtotalVenda > 0m ? descontoVenda * subtotalItem / subtotalVenda : 0m;
        return ArredondamentoHelper.Centavos(Math.Max(0m, subtotalItem - rateioDescontoVenda) / quantidadeItem);
    }
}
