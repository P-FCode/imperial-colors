using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
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

    /// <summary>
    /// Mesma agregação de <c>VendaRepository.ObterResumoDiarioAsync</c>, aplicada à venda de
    /// rua: uma linha por dia com movimento, somada pelo banco.
    ///
    /// Duas diferenças em relação ao balcão, que vêm do próprio modelo: venda externa não tem
    /// status (não existe cancelamento — o registro é excluído), então não há filtro de
    /// situação; e o item pode ser avulso (<c>ProdutoId</c> nulo, digitado na rua), caso em
    /// que não há custo cadastrado para descontar — ele entra em <c>ItensSemCusto</c>, que é
    /// o que faz a tela avisar que o lucro do período está subestimado.
    /// </summary>
    public async Task<IReadOnlyList<ResumoVendasDiario>> ObterResumoDiarioAsync(
        DateTime inicio, DateTime fimExclusivo, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();

        var porVenda = await context.Set<VendaExterna>()
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.DataVenda < fimExclusivo)
            .GroupBy(v => v.DataVenda.Date)
            .Select(g => new
            {
                Data = g.Key,
                Quantidade = g.Count(),
                Faturamento = g.Sum(v => v.Total)
            })
            .ToListAsync(cancellationToken);

        // IgnoreQueryFilters no produto: produto inativado depois da venda continua tendo o
        // custo que valeu naquele dia — sem isso o lucro do passado mudaria sozinho quando
        // alguém desativa um item do catálogo.
        var porItem = await (
            from item in context.Set<ItemVendaExterna>().AsNoTracking()
            join produto in context.Set<Produto>().IgnoreQueryFilters().AsNoTracking()
                on item.ProdutoId equals produto.Id into correspondentes
            from produto in correspondentes.DefaultIfEmpty()
            where item.VendaExterna.DataVenda >= inicio && item.VendaExterna.DataVenda < fimExclusivo
            group new { item, produto } by item.VendaExterna.DataVenda.Date into g
            select new
            {
                Data = g.Key,
                Custo = g.Sum(x => x.produto != null && x.produto.Custo != null
                    ? x.produto.Custo.Value * x.item.Quantidade
                    : 0m),
                ItensSemCusto = g.Count(x => x.produto == null || x.produto.Custo == null)
            }).ToListAsync(cancellationToken);

        var custosPorDia = porItem.ToDictionary(x => x.Data);

        return porVenda
            .Select(v =>
            {
                custosPorDia.TryGetValue(v.Data, out var c);
                return new ResumoVendasDiario
                {
                    Data = v.Data,
                    QuantidadeVendas = v.Quantidade,
                    Faturamento = v.Faturamento,
                    Custo = c?.Custo ?? 0m,
                    ItensSemCusto = c?.ItensSemCusto ?? 0
                };
            })
            .OrderBy(r => r.Data)
            .ToList();
    }

    public async Task<VendaExterna?> ObterComItensAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<VendaExterna>()
            .AsNoTracking()
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<VendaExterna>> ObterPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<VendaExterna>()
            .AsNoTracking()
            .Include(v => v.Itens)
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<VendaExterna> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<VendaExterna>().AsNoTracking().Include(v => v.Itens).AsQueryable();

        if (!string.IsNullOrWhiteSpace(termoBusca))
        {
            var termo = termoBusca.Trim();
            query = query.Where(v =>
                EF.Functions.ILike(v.NumeroVendaExterna, $"%{termo}%") ||
                (v.Observacoes != null && EF.Functions.ILike(v.Observacoes, $"%{termo}%")));
        }

        var total = await query.CountAsync(cancellationToken);
        var itens = await query
            .OrderByDescending(v => v.DataVenda)
            .ThenByDescending(v => v.Id)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .ToListAsync(cancellationToken);

        return (itens, total);
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
        return await ExecutarEmTransacaoAsync(async context =>
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
                var nomeProduto = await context.Set<Produto>()
                    .Where(p => p.Id == item.ProdutoId!.Value)
                    .Select(p => p.Nome)
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new DomainException($"Produto (Id={item.ProdutoId}) não encontrado.");

                var (qtdAnterior, qtdAtual) = await EstoqueAtomicoHelper.BaixarAsync(
                    context, item.ProdutoId!.Value, item.Quantidade, nomeProduto, cancellationToken);

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = item.ProdutoId!.Value,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    QuantidadeAnterior = qtdAnterior,
                    QuantidadeAtual = qtdAtual,
                    Motivo = $"Venda externa #{venda.NumeroVendaExterna}",
                    VendaExternaId = venda.Id,
                    Usuario = usuario
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            return venda;
        }, cancellationToken);
    }

    public async Task<VendaExterna> AtualizarTransacionalAsync(
        int vendaId,
        string? observacoes,
        IReadOnlyList<ItemVendaExterna> itens,
        string? usuario,
        CancellationToken cancellationToken = default)
    {
        var idAtualizado = await ExecutarEmTransacaoAsync(async context =>
        {
            var venda = await context.Set<VendaExterna>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda externa com Id {vendaId} não encontrada.");

            ValidarItens(itens);
            await GarantirQueEdicaoPreservaTrocasAsync(context, vendaId, itens, cancellationToken);

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
            venda.AtualizadoEm = Relogio.Agora;

            await context.SaveChangesAsync(cancellationToken);
            return venda.Id;
        }, cancellationToken);

        // Releitura DEPOIS da transação: ObterComItensAsync abre o próprio contexto pelo
        // factory, então precisa que o commit já tenha acontecido para enxergar as mudanças.
        // Antes ficava dentro do bloco try — funcionava porque vinha após o Commit, mas por
        // um fio; com a reexecução da estratégia, uma releitura dentro do delegate seria
        // refeita a cada tentativa sem necessidade.
        return (await ObterComItensAsync(idAtualizado, cancellationToken))!;
    }

    public Task ExcluirFisicamenteTransacionalAsync(int id, CancellationToken cancellationToken = default)
    {
        return ExecutarEmTransacaoAsync(async context =>
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
        }, cancellationToken);
    }

    public async Task<bool> PossuiTrocasAsync(int vendaExternaId, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Troca>()
            .AnyAsync(t => t.VendaExternaOrigemId == vendaExternaId, cancellationToken);
    }

    // Uma edição que deixa menos unidades do que já voltou por troca estornaria de novo o que a troca já repôs.
    private static async Task GarantirQueEdicaoPreservaTrocasAsync(
        AppDbContext context, int vendaExternaId, IReadOnlyList<ItemVendaExterna> itens, CancellationToken cancellationToken)
    {
        // Mesma chave usada por TrocaRepository: edição e troca da mesma venda não correm em paralelo.
        var chaveLock = $"troca_venda_externa:{vendaExternaId}";
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({chaveLock}))", cancellationToken);

        var devolvidos = await context.Set<Troca>()
            .IgnoreQueryFilters()
            .Where(t => t.VendaExternaOrigemId == vendaExternaId)
            .GroupBy(t => t.ProdutoDevolvidoId)
            .Select(g => new { ProdutoId = g.Key, Quantidade = g.Sum(t => t.QuantidadeDevolvida) })
            .ToListAsync(cancellationToken);

        foreach (var devolvido in devolvidos)
        {
            var quantidadeAposEdicao = itens.Where(i => i.ProdutoId == devolvido.ProdutoId).Sum(i => i.Quantidade);
            if (quantidadeAposEdicao >= devolvido.Quantidade)
                continue;

            var nome = await context.Set<Produto>().IgnoreQueryFilters()
                .Where(p => p.Id == devolvido.ProdutoId)
                .Select(p => p.Nome)
                .FirstOrDefaultAsync(cancellationToken) ?? $"produto Id {devolvido.ProdutoId}";

            throw new DomainException(
                $"'{nome}' já teve {devolvido.Quantidade:0.###} unidade(s) devolvida(s) em troca nesta venda externa; " +
                $"a quantidade vendida não pode ficar abaixo disso (ficaria {quantidadeAposEdicao:0.###}).");
        }
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
        var nomeProduto = await context.Set<Produto>()
            .Where(p => p.Id == produtoId)
            .Select(p => p.Nome)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new DomainException($"Produto (Id={produtoId}) não encontrado.");

        var (qtdAnterior, qtdAtual) = await EstoqueAtomicoHelper.BaixarAsync(
            context, produtoId, quantidade, nomeProduto, cancellationToken);

        context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Saida,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeAtual = qtdAtual,
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
        var (qtdAnterior, qtdAtual) = await EstoqueAtomicoHelper.ReporAsync(
            context, produtoId, quantidade, cancellationToken);

        context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
        {
            ProdutoId = produtoId,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = quantidade,
            QuantidadeAnterior = qtdAnterior,
            QuantidadeAtual = qtdAtual,
            Motivo = motivo,
            VendaExternaId = vendaExternaId,
            Usuario = usuario
        });
    }
}
