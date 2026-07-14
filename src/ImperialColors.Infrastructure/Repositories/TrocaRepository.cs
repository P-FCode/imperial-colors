using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Repositories;

public class TrocaRepository : RepositoryBase<Troca>, ITrocaRepository
{
    public TrocaRepository(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<TrocaRepository> logger)
        : base(contextFactory, logger) { }

    public async Task<IEnumerable<Troca>> ObterPorVendaAsync(int vendaId)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Troca>()
            .AsNoTracking()
            .Include(t => t.ProdutoDevolvido)
            .Include(t => t.ProdutoNovo)
            .Include(t => t.VendaOrigem)
            .Where(t => t.VendaOrigemId == vendaId)
            .OrderByDescending(t => t.DataTroca)
            .ToListAsync();
    }

    public async Task RegistrarTrocaTransacionalAsync(
        Troca troca,
        Produto produtoDevolvido,
        Produto produtoNovo,
        bool retornarAoEstoque,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Recarrega os produtos dentro do mesmo contexto para tracking correto
            var prodDev = await context.Set<Produto>()
                .FirstOrDefaultAsync(p => p.Id == produtoDevolvido.Id, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Produto devolvido (Id={produtoDevolvido.Id}) não encontrado.");

            var prodNovo = await context.Set<Produto>()
                .FirstOrDefaultAsync(p => p.Id == produtoNovo.Id, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Novo produto (Id={produtoNovo.Id}) não encontrado.");

            var venda = await context.Set<Venda>()
                .FirstOrDefaultAsync(v => v.Id == troca.VendaOrigemId, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException("Venda não encontrada.");

            // Controle de estoque: entrada do devolvido (se checkbox ativo)
            if (retornarAoEstoque)
            {
                var qtdAntesDev = prodDev.QuantidadeEstoque;
                prodDev.QuantidadeEstoque += troca.QuantidadeDevolvida;

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = prodDev.Id,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = troca.QuantidadeDevolvida,
                    QuantidadeAnterior = qtdAntesDev,
                    QuantidadeAtual = prodDev.QuantidadeEstoque,
                    Motivo = $"Troca - item devolvido da venda #{venda.NumeroVenda}",
                    VendaId = troca.VendaOrigemId
                });
            }

            // Controle de estoque: saída do novo item
            if (prodNovo.QuantidadeEstoque < troca.QuantidadeNova)
                throw new Domain.Exceptions.DomainException(
                    $"Estoque insuficiente para '{prodNovo.Nome}'. Disponível: {prodNovo.QuantidadeEstoque}.");

            var qtdAntesNovo = prodNovo.QuantidadeEstoque;
            prodNovo.QuantidadeEstoque -= troca.QuantidadeNova;

            context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
            {
                ProdutoId = prodNovo.Id,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = troca.QuantidadeNova,
                QuantidadeAnterior = qtdAntesNovo,
                QuantidadeAtual = prodNovo.QuantidadeEstoque,
                Motivo = $"Troca - novo item entregue - venda #{venda.NumeroVenda}",
                VendaId = troca.VendaOrigemId
            });

            // Salva a troca
            troca.Observacoes = string.IsNullOrWhiteSpace(troca.Observacoes)
                ? $"Troca vinculada à Venda ID {troca.VendaOrigemId}"
                : $"{troca.Observacoes} | Troca vinculada à Venda ID {troca.VendaOrigemId}";

            await context.Set<Troca>().AddAsync(troca, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RegistrarTrocaVendaExternaTransacionalAsync(
        Troca troca,
        Produto produtoDevolvido,
        Produto produtoNovo,
        bool retornarAoEstoque,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var prodDev = await context.Set<Produto>()
                .FirstOrDefaultAsync(p => p.Id == produtoDevolvido.Id, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Produto devolvido (Id={produtoDevolvido.Id}) não encontrado.");

            var prodNovo = await context.Set<Produto>()
                .FirstOrDefaultAsync(p => p.Id == produtoNovo.Id, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Novo produto (Id={produtoNovo.Id}) não encontrado.");

            var vendaExterna = await context.Set<VendaExterna>()
                .FirstOrDefaultAsync(v => v.Id == troca.VendaExternaOrigemId, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException("Venda externa não encontrada.");

            if (retornarAoEstoque)
            {
                var qtdAntesDev = prodDev.QuantidadeEstoque;
                prodDev.QuantidadeEstoque += troca.QuantidadeDevolvida;

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = prodDev.Id,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = troca.QuantidadeDevolvida,
                    QuantidadeAnterior = qtdAntesDev,
                    QuantidadeAtual = prodDev.QuantidadeEstoque,
                    Motivo = $"Troca - item devolvido da venda externa #{vendaExterna.NumeroVendaExterna}",
                    VendaExternaId = troca.VendaExternaOrigemId
                });
            }

            if (prodNovo.QuantidadeEstoque < troca.QuantidadeNova)
                throw new Domain.Exceptions.DomainException(
                    $"Estoque insuficiente para '{prodNovo.Nome}'. Disponível: {prodNovo.QuantidadeEstoque}.");

            var qtdAntesNovo = prodNovo.QuantidadeEstoque;
            prodNovo.QuantidadeEstoque -= troca.QuantidadeNova;

            context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
            {
                ProdutoId = prodNovo.Id,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = troca.QuantidadeNova,
                QuantidadeAnterior = qtdAntesNovo,
                QuantidadeAtual = prodNovo.QuantidadeEstoque,
                Motivo = $"Troca - novo item entregue - venda externa #{vendaExterna.NumeroVendaExterna}",
                VendaExternaId = troca.VendaExternaOrigemId
            });

            troca.Observacoes = string.IsNullOrWhiteSpace(troca.Observacoes)
                ? $"Troca vinculada à Venda Externa ID {troca.VendaExternaOrigemId}"
                : $"{troca.Observacoes} | Troca vinculada à Venda Externa ID {troca.VendaExternaOrigemId}";

            await context.Set<Troca>().AddAsync(troca, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
