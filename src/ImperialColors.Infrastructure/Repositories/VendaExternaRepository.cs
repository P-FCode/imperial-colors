using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Repositories;

public class VendaExternaRepository : RepositoryBase<VendaExterna>, IVendaExternaRepository
{
    public VendaExternaRepository(
        IDbContextFactory<AppDbContext> contextFactory,
        ILogger<VendaExternaRepository> logger)
        : base(contextFactory, logger) { }

    public async Task<IEnumerable<VendaExterna>> ObterTodosComItensAsync(CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<VendaExterna>()
            .AsNoTracking()
            .Include(v => v.Itens)
            .OrderByDescending(v => v.DataVenda)
            .ThenByDescending(v => v.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<VendaExterna?> ObterComItensAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<VendaExterna>()
            .AsNoTracking()
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<string> GerarNumeroVendaExternaAsync(CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var hoje = DateTime.Today;
        var prefixo = $"EXT-{hoje:yyyyMMdd}";
        var ultima = await context.Set<VendaExterna>()
            .Where(v => v.NumeroVendaExterna.StartsWith(prefixo))
            .OrderByDescending(v => v.NumeroVendaExterna)
            .FirstOrDefaultAsync(cancellationToken);

        var sequencial = 1;
        if (ultima is not null)
        {
            var partes = ultima.NumeroVendaExterna.Split('-');
            if (partes.Length == 3 && int.TryParse(partes[2], out var seq))
                sequencial = seq + 1;
        }

        return $"{prefixo}-{sequencial:D4}";
    }

    public async Task<VendaExterna> RegistrarTransacionalAsync(
        VendaExterna venda,
        IReadOnlyList<ItemVendaExterna> itens,
        string? usuario,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            venda.Usuario = usuario;
            venda.Itens = itens.ToList();
            venda.CalcularTotais();

            foreach (var item in itens)
            {
                item.CalcularSubtotal();
                if (item.Quantidade <= 0)
                    throw new DomainException($"Quantidade inválida para '{item.NomeProduto}'.");
                if (item.PrecoUnitario < 0)
                    throw new DomainException($"Preço unitário inválido para '{item.NomeProduto}'.");
            }

            await context.Set<VendaExterna>().AddAsync(venda, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            foreach (var item in itens.Where(i => i.ProdutoId.HasValue))
            {
                var produto = await context.Set<Produto>()
                    .FirstOrDefaultAsync(p => p.Id == item.ProdutoId!.Value, cancellationToken)
                    ?? throw new DomainException($"Produto (Id={item.ProdutoId}) não encontrado.");

                if (produto.QuantidadeEstoque < item.Quantidade)
                    throw new DomainException(
                        $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEstoque}.");

                var qtdAnterior = produto.QuantidadeEstoque;
                produto.QuantidadeEstoque -= item.Quantidade;

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = produto.Id,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    QuantidadeAnterior = qtdAnterior,
                    QuantidadeAtual = produto.QuantidadeEstoque,
                    Motivo = $"Venda externa #{venda.NumeroVendaExterna}",
                    VendaExternaId = venda.Id,
                    Usuario = usuario
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return venda;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<VendaExterna> AtualizarTransacionalAsync(
        int vendaId,
        string? observacoes,
        IReadOnlyList<ItemVendaExterna> itens,
        string? usuario,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var venda = await context.Set<VendaExterna>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda externa com Id {vendaId} não encontrada.");

            ValidarItens(itens);

            var itensAntigos = venda.Itens.ToDictionary(i => i.Id);
            var idsNovos = itens.Where(i => i.Id > 0).Select(i => i.Id).ToHashSet();
            var numero = venda.NumeroVendaExterna;

            foreach (var antigo in itensAntigos.Values.Where(i => !idsNovos.Contains(i.Id)))
            {
                if (antigo.ProdutoId.HasValue)
                    await ReporEstoqueAsync(context, antigo.ProdutoId.Value, antigo.Quantidade,
                        $"Estorno item removido - edição venda externa #{numero}", venda.Id, usuario, cancellationToken);

                context.Set<ItemVendaExterna>().Remove(antigo);
            }

            foreach (var item in itens)
            {
                item.CalcularSubtotal();

                if (item.Id > 0 && itensAntigos.TryGetValue(item.Id, out var antigo))
                {
                    await AplicarAjusteEdicaoItemAsync(context, antigo, item, numero, venda.Id, usuario, cancellationToken);

                    antigo.ProdutoId = item.ProdutoId;
                    antigo.NomeProduto = item.NomeProduto;
                    antigo.CodigoBarras = item.CodigoBarras;
                    antigo.Quantidade = item.Quantidade;
                    antigo.PrecoBase = item.PrecoBase;
                    antigo.PrecoUnitario = item.PrecoUnitario;
                    antigo.Subtotal = item.Subtotal;
                }
                else
                {
                    if (item.ProdutoId.HasValue)
                    {
                        await BaixarEstoqueAsync(context, item.ProdutoId.Value, item.Quantidade,
                            $"Venda externa #{numero} (item adicionado na edição)", venda.Id, usuario, cancellationToken);
                    }

                    context.Set<ItemVendaExterna>().Add(new ItemVendaExterna
                    {
                        VendaExternaId = venda.Id,
                        ProdutoId = item.ProdutoId,
                        NomeProduto = item.NomeProduto,
                        CodigoBarras = item.CodigoBarras,
                        Quantidade = item.Quantidade,
                        PrecoBase = item.PrecoBase,
                        PrecoUnitario = item.PrecoUnitario,
                        Subtotal = item.Subtotal
                    });
                }
            }

            venda.Observacoes = string.IsNullOrWhiteSpace(observacoes) ? null : observacoes.Trim();
            venda.Subtotal = itens.Sum(i => i.Quantidade * i.PrecoUnitario);
            venda.Total = venda.Subtotal;
            venda.AtualizadoEm = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await ObterComItensAsync(venda.Id, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExcluirFisicamenteTransacionalAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var venda = await context.Set<VendaExterna>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
                ?? throw new DomainException($"Venda externa com Id {id} não encontrada.");

            if (await context.Set<Troca>().AnyAsync(t => t.VendaExternaOrigemId == id, cancellationToken))
                throw new DomainException("Esta venda externa possui trocas registradas e não pode ser excluída.");

            foreach (var item in venda.Itens.Where(i => i.ProdutoId.HasValue))
            {
                await ReporEstoqueAsync(context, item.ProdutoId!.Value, item.Quantidade,
                    $"Estorno exclusão venda externa #{venda.NumeroVendaExterna}", venda.Id, venda.Usuario, cancellationToken);
            }

            context.Set<VendaExterna>().Remove(venda);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> PossuiTrocasAsync(int vendaExternaId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Troca>()
            .AnyAsync(t => t.VendaExternaOrigemId == vendaExternaId, cancellationToken);
    }

    private static void ValidarItens(IReadOnlyList<ItemVendaExterna> itens)
    {
        if (itens.Count == 0)
            throw new DomainException("Adicione pelo menos um item à venda externa.");

        foreach (var item in itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
                throw new DomainException("Todos os itens devem ter um nome de produto.");
            if (item.Quantidade <= 0)
                throw new DomainException($"Quantidade inválida para '{item.NomeProduto}'.");
            if (item.PrecoUnitario < 0)
                throw new DomainException($"Preço unitário inválido para '{item.NomeProduto}'.");
        }
    }

    private static async Task AplicarAjusteEdicaoItemAsync(
        AppDbContext context,
        ItemVendaExterna antigo,
        ItemVendaExterna novo,
        string numeroVenda,
        int vendaExternaId,
        string? usuario,
        CancellationToken cancellationToken)
    {
        if (antigo.ProdutoId == novo.ProdutoId)
        {
            if (!antigo.ProdutoId.HasValue)
                return;

            var delta = novo.Quantidade - antigo.Quantidade;
            if (delta == 0)
                return;

            if (delta > 0)
            {
                await BaixarEstoqueAsync(context, antigo.ProdutoId.Value, delta,
                    $"Ajuste edição venda externa #{numeroVenda}", vendaExternaId, usuario, cancellationToken);
            }
            else
            {
                await ReporEstoqueAsync(context, antigo.ProdutoId.Value, Math.Abs(delta),
                    $"Ajuste edição venda externa #{numeroVenda}", vendaExternaId, usuario, cancellationToken);
            }

            return;
        }

        if (antigo.ProdutoId.HasValue)
        {
            await ReporEstoqueAsync(context, antigo.ProdutoId.Value, antigo.Quantidade,
                $"Estorno troca de item - edição venda externa #{numeroVenda}", vendaExternaId, usuario, cancellationToken);
        }

        if (novo.ProdutoId.HasValue)
        {
            await BaixarEstoqueAsync(context, novo.ProdutoId.Value, novo.Quantidade,
                $"Baixa item alterado - edição venda externa #{numeroVenda}", vendaExternaId, usuario, cancellationToken);
        }
    }

    private static async Task BaixarEstoqueAsync(
        AppDbContext context,
        int produtoId,
        decimal quantidade,
        string motivo,
        int vendaExternaId,
        string? usuario,
        CancellationToken cancellationToken)
    {
        var produto = await context.Set<Produto>()
            .FirstOrDefaultAsync(p => p.Id == produtoId, cancellationToken)
            ?? throw new DomainException($"Produto (Id={produtoId}) não encontrado.");

        if (produto.QuantidadeEstoque < quantidade)
            throw new DomainException(
                $"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEstoque}.");

        var qtdAnterior = produto.QuantidadeEstoque;
        produto.QuantidadeEstoque -= quantidade;

        context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
        {
            ProdutoId = produto.Id,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeAtual = produto.QuantidadeEstoque,
            Motivo = motivo,
            VendaExternaId = vendaExternaId,
            Usuario = usuario
        });
    }

    private static async Task ReporEstoqueAsync(
        AppDbContext context,
        int produtoId,
        decimal quantidade,
        string motivo,
        int vendaExternaId,
        string? usuario,
        CancellationToken cancellationToken)
    {
        var produto = await context.Set<Produto>()
            .FirstOrDefaultAsync(p => p.Id == produtoId, cancellationToken)
            ?? throw new DomainException($"Produto (Id={produtoId}) não encontrado.");

        var qtdAnterior = produto.QuantidadeEstoque;
        produto.QuantidadeEstoque += quantidade;

        context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
        {
            ProdutoId = produto.Id,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeAtual = produto.QuantidadeEstoque,
            Motivo = motivo,
            VendaExternaId = vendaExternaId,
            Usuario = usuario
        });
    }
}
