using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
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
        await ExecutarEmTransacaoAsync(async context =>
        {
            // Nomes apenas para validar existência e compor mensagens de erro amigáveis —
            // a baixa/reposição real é feita atomicamente pelo EstoqueAtomicoHelper abaixo.
            var nomeProdDevolvido = await context.Set<Produto>()
                .Where(p => p.Id == produtoDevolvido.Id)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Produto devolvido (Id={produtoDevolvido.Id}) não encontrado.");

            var nomeProdNovo = await context.Set<Produto>()
                .Where(p => p.Id == produtoNovo.Id)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Novo produto (Id={produtoNovo.Id}) não encontrado.");

            var venda = await context.Set<Venda>()
                .FirstOrDefaultAsync(v => v.Id == troca.VendaOrigemId, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException("Venda não encontrada.");

            await TravarTrocasDaOrigemAsync(context, $"troca_venda:{venda.Id}", cancellationToken);

            var vendido = await context.Set<ItemVenda>()
                .Where(i => i.VendaId == venda.Id && i.ProdutoId == produtoDevolvido.Id)
                .SumAsync(i => i.Quantidade, cancellationToken);

            var jaDevolvido = await context.Set<Troca>()
                .IgnoreQueryFilters()
                .Where(t => t.VendaOrigemId == venda.Id && t.ProdutoDevolvidoId == produtoDevolvido.Id)
                .SumAsync(t => t.QuantidadeDevolvida, cancellationToken);

            GarantirSaldoParaDevolucao(nomeProdDevolvido, vendido, jaDevolvido, troca.QuantidadeDevolvida);

            // Controle de estoque: entrada do devolvido (se checkbox ativo)
            if (retornarAoEstoque)
            {
                var (qtdAntesDev, qtdDepoisDev) = await EstoqueAtomicoHelper.ReporAsync(
                    context, produtoDevolvido.Id, troca.QuantidadeDevolvida, cancellationToken);

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = produtoDevolvido.Id,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = troca.QuantidadeDevolvida,
                    QuantidadeAnterior = qtdAntesDev,
                    QuantidadeAtual = qtdDepoisDev,
                    Motivo = $"Troca - item devolvido da venda #{venda.NumeroVenda}",
                    VendaId = troca.VendaOrigemId
                });
            }

            // Controle de estoque: saída do novo item (guardado atomicamente contra estoque negativo)
            var (qtdAntesNovo, qtdDepoisNovo) = await EstoqueAtomicoHelper.BaixarAsync(
                context, produtoNovo.Id, troca.QuantidadeNova, nomeProdNovo, cancellationToken);

            context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
            {
                ProdutoId = produtoNovo.Id,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = troca.QuantidadeNova,
                QuantidadeAnterior = qtdAntesNovo,
                QuantidadeAtual = qtdDepoisNovo,
                Motivo = $"Troca - novo item entregue - venda #{venda.NumeroVenda}",
                VendaId = troca.VendaOrigemId
            });

            // Salva a troca
            troca.Observacoes = string.IsNullOrWhiteSpace(troca.Observacoes)
                ? $"Troca vinculada à Venda ID {troca.VendaOrigemId}"
                : $"{troca.Observacoes} | Troca vinculada à Venda ID {troca.VendaOrigemId}";

            await context.Set<Troca>().AddAsync(troca, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task RegistrarTrocaVendaExternaTransacionalAsync(
        Troca troca,
        Produto produtoDevolvido,
        Produto produtoNovo,
        bool retornarAoEstoque,
        CancellationToken cancellationToken = default)
    {
        await ExecutarEmTransacaoAsync(async context =>
        {
            var nomeProdDevolvido = await context.Set<Produto>()
                .Where(p => p.Id == produtoDevolvido.Id)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Produto devolvido (Id={produtoDevolvido.Id}) não encontrado.");

            var nomeProdNovo = await context.Set<Produto>()
                .Where(p => p.Id == produtoNovo.Id)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new Domain.Exceptions.DomainException($"Novo produto (Id={produtoNovo.Id}) não encontrado.");

            var vendaExterna = await context.Set<VendaExterna>()
                .FirstOrDefaultAsync(v => v.Id == troca.VendaExternaOrigemId, cancellationToken)
                ?? throw new Domain.Exceptions.DomainException("Venda externa não encontrada.");

            await TravarTrocasDaOrigemAsync(context, $"troca_venda_externa:{vendaExterna.Id}", cancellationToken);

            var vendido = await context.Set<ItemVendaExterna>()
                .Where(i => i.VendaExternaId == vendaExterna.Id && i.ProdutoId == produtoDevolvido.Id)
                .SumAsync(i => i.Quantidade, cancellationToken);

            var jaDevolvido = await context.Set<Troca>()
                .IgnoreQueryFilters()
                .Where(t => t.VendaExternaOrigemId == vendaExterna.Id && t.ProdutoDevolvidoId == produtoDevolvido.Id)
                .SumAsync(t => t.QuantidadeDevolvida, cancellationToken);

            GarantirSaldoParaDevolucao(nomeProdDevolvido, vendido, jaDevolvido, troca.QuantidadeDevolvida);

            if (retornarAoEstoque)
            {
                var (qtdAntesDev, qtdDepoisDev) = await EstoqueAtomicoHelper.ReporAsync(
                    context, produtoDevolvido.Id, troca.QuantidadeDevolvida, cancellationToken);

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = produtoDevolvido.Id,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = troca.QuantidadeDevolvida,
                    QuantidadeAnterior = qtdAntesDev,
                    QuantidadeAtual = qtdDepoisDev,
                    Motivo = $"Troca - item devolvido da venda externa #{vendaExterna.NumeroVendaExterna}",
                    VendaExternaId = troca.VendaExternaOrigemId
                });
            }

            var (qtdAntesNovo, qtdDepoisNovo) = await EstoqueAtomicoHelper.BaixarAsync(
                context, produtoNovo.Id, troca.QuantidadeNova, nomeProdNovo, cancellationToken);

            context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
            {
                ProdutoId = produtoNovo.Id,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = troca.QuantidadeNova,
                QuantidadeAnterior = qtdAntesNovo,
                QuantidadeAtual = qtdDepoisNovo,
                Motivo = $"Troca - novo item entregue - venda externa #{vendaExterna.NumeroVendaExterna}",
                VendaExternaId = troca.VendaExternaOrigemId
            });

            troca.Observacoes = string.IsNullOrWhiteSpace(troca.Observacoes)
                ? $"Troca vinculada à Venda Externa ID {troca.VendaExternaOrigemId}"
                : $"{troca.Observacoes} | Troca vinculada à Venda Externa ID {troca.VendaExternaOrigemId}";

            await context.Set<Troca>().AddAsync(troca, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    // Serializa trocas da mesma venda: duas telas abertas não podem, juntas, devolver mais do que foi vendido.
    private static Task TravarTrocasDaOrigemAsync(AppDbContext context, string chave, CancellationToken cancellationToken)
        => context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({chave}))", cancellationToken);

    private static void GarantirSaldoParaDevolucao(string nomeProduto, decimal vendido, decimal jaDevolvido, decimal devolvendoAgora)
    {
        if (jaDevolvido + devolvendoAgora <= vendido)
            return;

        var restante = Math.Max(0m, vendido - jaDevolvido);
        throw new Domain.Exceptions.DomainException(
            $"Não é possível devolver {devolvendoAgora:0.###} de '{nomeProduto}': foram vendidas {vendido:0.###} " +
            $"e {jaDevolvido:0.###} já foram devolvidas em trocas anteriores. Restam {restante:0.###} para troca.");
    }
}
