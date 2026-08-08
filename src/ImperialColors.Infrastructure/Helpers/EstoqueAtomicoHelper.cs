using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Helpers;

/// <summary>
/// Aplica variações de <see cref="Produto.QuantidadeEstoque"/> como um UPDATE atômico
/// e guardado no PostgreSQL (<c>ExecuteUpdateAsync</c>), em vez do padrão clássico
/// "ler a entidade em C#, subtrair/somar, chamar SaveChanges" — que sofre lost update
/// quando dois PDVs (ou uma venda e uma troca) alteram o mesmo produto ao mesmo tempo:
/// os dois leem o valor antigo, os dois escrevem, e uma das baixas se perde (ou o
/// estoque fica negativo).
///
/// O UPDATE gerado equivale a:
///   UPDATE produtos SET quantidade_estoque = quantidade_estoque - @qtd
///   WHERE id = @id AND quantidade_estoque >= @qtd
/// A condição "quantidade_estoque >= @qtd" no WHERE é resolvida atomicamente pelo
/// próprio PostgreSQL (MVCC + lock de linha da própria instrução UPDATE) — não existe
/// janela entre "ler o estoque" e "gravar o novo valor" onde outra transação possa
/// intercalar uma escrita concorrente.
///
/// Sempre chamar dentro de uma transação já aberta (<c>context.Database.BeginTransactionAsync</c>)
/// no mesmo <paramref name="context"/> usado para o restante da operação (venda, troca, etc.),
/// para que a baixa de estoque e a gravação da venda sejam atômicas em conjunto.
/// </summary>
public static class EstoqueAtomicoHelper
{
    /// <summary>
    /// Dá baixa (saída) atômica no estoque de um produto. Lança <see cref="DomainException"/>
    /// se o produto não existir ou se o estoque disponível for insuficiente no exato momento
    /// da escrita (mesmo que uma validação otimista anterior tenha passado).
    /// </summary>
    public static async Task<(decimal QuantidadeAnterior, decimal QuantidadeAtual)> BaixarAsync(
        AppDbContext context,
        int produtoId,
        decimal quantidade,
        string? nomeProdutoParaErro = null,
        CancellationToken cancellationToken = default)
    {
        var rotulo = nomeProdutoParaErro ?? $"produto Id {produtoId}";

        if (quantidade <= 0)
            throw new DomainException($"Quantidade inválida para '{rotulo}'.");

        var linhasAfetadas = await context.Set<Produto>()
            .Where(p => p.Id == produtoId && p.QuantidadeEstoque >= quantidade)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.QuantidadeEstoque, p => p.QuantidadeEstoque - quantidade)
                .SetProperty(p => p.AtualizadoEm, DateTime.UtcNow), cancellationToken);

        if (linhasAfetadas == 0)
        {
            var disponivel = await context.Set<Produto>()
                .Where(p => p.Id == produtoId)
                .Select(p => (decimal?)p.QuantidadeEstoque)
                .FirstOrDefaultAsync(cancellationToken);

            if (disponivel is null)
                throw new DomainException($"Produto (Id={produtoId}) não encontrado.");

            throw new DomainException(
                $"Estoque insuficiente para '{rotulo}'. Disponível: {disponivel.Value}.");
        }

        var atual = await context.Set<Produto>()
            .Where(p => p.Id == produtoId)
            .Select(p => p.QuantidadeEstoque)
            .FirstAsync(cancellationToken);

        return (QuantidadeAnterior: atual + quantidade, QuantidadeAtual: atual);
    }

    /// <summary>
    /// Repõe (entrada) atômica no estoque de um produto (devolução, cancelamento, estorno).
    /// </summary>
    public static async Task<(decimal QuantidadeAnterior, decimal QuantidadeAtual)> ReporAsync(
        AppDbContext context,
        int produtoId,
        decimal quantidade,
        CancellationToken cancellationToken = default)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade de reposição de estoque inválida.");

        var linhasAfetadas = await context.Set<Produto>()
            .Where(p => p.Id == produtoId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.QuantidadeEstoque, p => p.QuantidadeEstoque + quantidade)
                .SetProperty(p => p.AtualizadoEm, DateTime.UtcNow), cancellationToken);

        if (linhasAfetadas == 0)
            throw new DomainException($"Produto (Id={produtoId}) não encontrado.");

        var atual = await context.Set<Produto>()
            .Where(p => p.Id == produtoId)
            .Select(p => p.QuantidadeEstoque)
            .FirstAsync(cancellationToken);

        return (QuantidadeAnterior: atual - quantidade, QuantidadeAtual: atual);
    }
}
