using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Repositories;

public class OrcamentoRepository : RepositoryBase<Orcamento>, IOrcamentoRepository
{
    public OrcamentoRepository(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<OrcamentoRepository> logger)
        : base(contextFactory, logger) { }

    public async Task<(IReadOnlyList<Orcamento> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<Orcamento>().AsNoTracking().Include(o => o.Itens).AsQueryable();

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(o =>
                EF.Functions.ILike(o.NumeroOrcamento, $"%{termo}%") ||
                EF.Functions.ILike(o.NomeCliente, $"%{termo}%") ||
                (o.TelefoneCliente != null && EF.Functions.ILike(o.TelefoneCliente, $"%{termo}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderByDescending(o => o.DataOrcamento)
            .ThenByDescending(o => o.Id)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<Orcamento?> ObterComItensAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Orcamento>()
            .AsNoTracking()
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Orcamento> RegistrarTransacionalAsync(
        Orcamento orcamento,
        IReadOnlyList<ItemOrcamento> itens,
        CancellationToken cancellationToken = default)
    {
        var id = await ExecutarEmTransacaoAsync(async context =>
        {
            var prefixo = $"ORC-{Relogio.Agora:yyyyMMdd}";
            var chaveLock = $"orcamento_numero:{prefixo}";

            // Advisory lock transacional: serializa a geração do número de orçamento entre
            // PDVs concorrentes (liberado automaticamente no commit/rollback) — mesmo padrão
            // usado em VendaRepository.CriarComBaixaEstoqueTransacionalAsync. Sem isto, dois
            // registros simultâneos calculavam o mesmo próximo número e o segundo estourava
            // com DbUpdateException por violar o índice único de NumeroOrcamento.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({chaveLock}))", cancellationToken);

            // IgnoreQueryFilters: um orçamento excluído continua ocupando o número (o índice
            // único não conhece soft delete). Ignorar o filtro evita colisão ao gerar o próximo.
            var ultimo = await context.Set<Orcamento>()
                .IgnoreQueryFilters()
                .Where(o => o.NumeroOrcamento.StartsWith(prefixo))
                .OrderByDescending(o => o.NumeroOrcamento)
                .Select(o => o.NumeroOrcamento)
                .FirstOrDefaultAsync(cancellationToken);

            var sequencial = 1;
            if (ultimo is not null)
            {
                var partes = ultimo.Split('-');
                if (partes.Length == 3 && int.TryParse(partes[2], out var seq))
                    sequencial = seq + 1;
            }

            orcamento.NumeroOrcamento = $"{prefixo}-{sequencial:D4}";
            orcamento.Itens = itens.ToList();

            foreach (var item in orcamento.Itens)
                item.CalcularSubtotal();

            orcamento.CalcularTotais();

            await context.Set<Orcamento>().AddAsync(orcamento, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return orcamento.Id;
        }, cancellationToken);

        return (await ObterComItensAsync(id, cancellationToken))!;
    }

    public async Task<Orcamento> AtualizarTransacionalAsync(
        Orcamento orcamento,
        IReadOnlyList<ItemOrcamento> itens,
        CancellationToken cancellationToken = default)
    {
        var id = await ExecutarEmTransacaoAsync(async context =>
        {
            var existente = await context.Set<Orcamento>()
                .Include(o => o.Itens)
                .FirstOrDefaultAsync(o => o.Id == orcamento.Id, cancellationToken)
                ?? throw new DomainException($"Orçamento com Id {orcamento.Id} não encontrado.");

            // Troca a lista inteira em vez de casar item a item: orçamento não movimenta
            // estoque nem dinheiro, então não há nada para estornar — o histórico que importa
            // é o PDF já entregue ao cliente, não a linha antiga da grade.
            context.Set<ItemOrcamento>().RemoveRange(existente.Itens);

            foreach (var item in itens)
            {
                item.CalcularSubtotal();
                context.Set<ItemOrcamento>().Add(new ItemOrcamento
                {
                    OrcamentoId = existente.Id,
                    ProdutoId = item.ProdutoId,
                    NomeProduto = item.NomeProduto,
                    CodigoProduto = item.CodigoProduto,
                    Unidade = item.Unidade,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    Subtotal = item.Subtotal
                });
            }

            existente.ClienteId = orcamento.ClienteId;
            existente.NomeCliente = orcamento.NomeCliente;
            existente.TelefoneCliente = orcamento.TelefoneCliente;
            existente.DataValidade = orcamento.DataValidade;
            existente.Desconto = orcamento.Desconto;
            existente.Observacoes = orcamento.Observacoes;
            existente.Usuario = orcamento.Usuario;
            existente.Subtotal = itens.Sum(i => i.Subtotal);
            existente.Total = Math.Max(0, existente.Subtotal - existente.Desconto);
            existente.AtualizadoEm = Relogio.Agora;

            await context.SaveChangesAsync(cancellationToken);
            return existente.Id;
        }, cancellationToken);

        return (await ObterComItensAsync(id, cancellationToken))!;
    }

    public async Task<Orcamento> AlterarStatusAsync(
        int id, StatusOrcamento status, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();

        var orcamento = await context.Set<Orcamento>()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new DomainException($"Orçamento com Id {id} não encontrado.");

        orcamento.Status = status;
        orcamento.AtualizadoEm = Relogio.Agora;
        await SalvarAlteracoesAsync(context);

        return (await ObterComItensAsync(id, cancellationToken))!;
    }
}
