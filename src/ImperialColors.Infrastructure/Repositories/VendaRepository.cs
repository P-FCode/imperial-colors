using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class VendaRepository : RepositoryBase<Venda>, IVendaRepository
{
    public VendaRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<Venda> CriarComBaixaEstoqueTransacionalAsync(Venda venda, CancellationToken cancellationToken = default)
    {
        if (venda.Itens.Count == 0)
            throw new DomainException("A venda deve ter pelo menos um item.");

        // Sob a estratégia de execução: o número da venda é gerado DENTRO do delegate, então
        // uma eventual reexecução por falha transitória recalcula o sequencial sob um novo
        // advisory lock, em vez de reaproveitar um número que outro PDV pode ter tomado nesse
        // meio-tempo. Ver RepositoryBase.ExecutarEmTransacaoAsync.
        return await ExecutarEmTransacaoAsync(async context =>
        {
            var prefixo = Relogio.Hoje.ToString("yyyyMMdd");
            var chaveLock = $"venda_numero:{prefixo}";

            // Advisory lock transacional: serializa a geração do número de venda entre
            // PDVs concorrentes (liberado automaticamente no commit/rollback), sem
            // bloquear a tabela inteira nem depender de retry em caso de colisão.
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({chaveLock}))", cancellationToken);

            // IgnoreQueryFilters: precisa considerar até vendas soft-deletadas (Ativo=false)
            // para não gerar um numero_venda que colida com o índice único da tabela.
            var ultimaVenda = await context.Set<Venda>()
                .IgnoreQueryFilters()
                .Where(v => v.NumeroVenda.StartsWith(prefixo))
                .OrderByDescending(v => v.NumeroVenda)
                .FirstOrDefaultAsync(cancellationToken);

            var sequencial = 1;
            if (ultimaVenda is not null)
            {
                var partes = ultimaVenda.NumeroVenda.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out var seq))
                    sequencial = seq + 1;
            }

            venda.NumeroVenda = $"{prefixo}-{sequencial:D4}";

            await context.Set<Venda>().AddAsync(venda, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var produtoIds = venda.Itens.Select(i => i.ProdutoId).Distinct().ToList();
            var nomesProdutos = await context.Set<Produto>()
                .IgnoreQueryFilters()
                .Where(p => produtoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Nome, cancellationToken);

            foreach (var item in venda.Itens)
            {
                var nomeProduto = nomesProdutos.GetValueOrDefault(item.ProdutoId, $"produto Id {item.ProdutoId}");
                var (quantidadeAnterior, quantidadeAtual) = await EstoqueAtomicoHelper.BaixarAsync(
                    context, item.ProdutoId, item.Quantidade, nomeProduto, cancellationToken);

                context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    QuantidadeAnterior = quantidadeAnterior,
                    QuantidadeAtual = quantidadeAtual,
                    Motivo = venda.ContingenciaId.HasValue
                        ? $"Venda #{venda.NumeroVenda} (sync contingência)"
                        : $"Venda #{venda.NumeroVenda}",
                    Usuario = venda.Usuario,
                    VendaId = venda.Id
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            return venda;
        }, cancellationToken);
    }

    public async Task<Venda?> ObterComItensAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Pagamentos)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Venda>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status == StatusVenda.Finalizada)
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync();
    }

    // Os dois totais abaixo usam intervalo MEIO-ABERTO [inicio, fimExclusivo). Antes eram
    // `v.DataVenda.Date == data.Date` e `v.DataVenda.Year == ano && .Month == mes`, que viram
    // DATE(data_venda) = ... e EXTRACT(... FROM data_venda) = ... no SQL — aplicar função
    // sobre a coluna torna o predicado não-sargável e o IX_vendas_data_venda deixa de ser
    // usado: o Postgres varre a tabela inteira e avalia linha a linha. Comparar a coluna
    // "crua" contra dois limites mantém o índice em jogo e, de quebra, deixa de perder o
    // último segundo do período (o `<=` com 23:59:59 excluía vendas em 23:59:59.5).
    public async Task<decimal> ObterTotalVendasDiaAsync(DateTime data)
    {
        var inicio = data.Date;
        var fimExclusivo = inicio.AddDays(1);

        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada &&
                        v.DataVenda >= inicio && v.DataVenda < fimExclusivo)
            .SumAsync(v => v.Total);
    }

    public async Task<decimal> ObterTotalVendasMesAsync(int ano, int mes)
    {
        var inicio = new DateTime(ano, mes, 1);
        var fimExclusivo = inicio.AddMonths(1);

        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada &&
                        v.DataVenda >= inicio && v.DataVenda < fimExclusivo)
            .SumAsync(v => v.Total);
    }

    /// <summary>
    /// Duas agregações no banco (uma no nível da venda, outra no nível do item) casadas por
    /// dia em memória. São dois SELECTs porque <c>Venda.Total</c> se repetiria em cada linha
    /// de item num único JOIN, inflando o faturamento — o clássico fan-out de agregar sobre
    /// junção 1-N. Cada consulta devolve no máximo uma linha por dia do intervalo.
    ///
    /// O custo vem de <c>Produto.Custo</c> com <c>IgnoreQueryFilters</c> de propósito: o custo
    /// histórico de um produto não deixa de existir porque ele foi inativado depois da venda.
    /// A versão anterior usava <c>Include(i =&gt; i.Produto)</c>, que aplica o filtro de
    /// soft-delete e devolvia <c>null</c> nesse caso — inativar um produto fazia o lucro de
    /// todas as vendas passadas dele subir retroativamente, contando o item como "sem custo".
    /// </summary>
    public async Task<IReadOnlyList<ResumoVendasDiario>> ObterResumoDiarioAsync(
        DateTime inicio, DateTime fimExclusivo, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();

        var porVenda = await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada &&
                        v.DataVenda >= inicio && v.DataVenda < fimExclusivo)
            .GroupBy(v => v.DataVenda.Date)
            .Select(g => new
            {
                Data = g.Key,
                Quantidade = g.Count(),
                Faturamento = g.Sum(v => v.Total)
            })
            .ToListAsync(cancellationToken);

        var porItem = await (
            from item in context.Set<ItemVenda>().AsNoTracking()
            join produto in context.Set<Produto>().IgnoreQueryFilters().AsNoTracking()
                on item.ProdutoId equals produto.Id into correspondentes
            from produto in correspondentes.DefaultIfEmpty()
            where item.Venda.Status == StatusVenda.Finalizada &&
                  item.Venda.DataVenda >= inicio && item.Venda.DataVenda < fimExclusivo
            group new { item, produto } by item.Venda.DataVenda.Date into g
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

    public async Task<(IReadOnlyList<Venda> Itens, int Total)> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default)
    {
        pagina = Math.Max(1, pagina);
        itensPorPagina = Math.Clamp(itensPorPagina, 1, 200);

        await using var context = ContextFactory.CreateDbContext();

        var noPeriodo = context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.DataVenda <= fim && v.Status != StatusVenda.Aberta);

        if (string.IsNullOrWhiteSpace(termoBusca))
        {
            var totalSemBusca = await noPeriodo.CountAsync(cancellationToken);
            var itensSemBusca = await noPeriodo
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.DataVenda)
                .Skip((pagina - 1) * itensPorPagina)
                .Take(itensPorPagina)
                .ToListAsync(cancellationToken);

            return (itensSemBusca, totalSemBusca);
        }

        var termo = termoBusca.Trim();

        // UNION de três ramos, cada um capaz de usar o próprio índice — em vez de um único
        // WHERE com "numero OR cliente.nome OR cupom".
        //
        // O problema do OR único não era só a falta de índice: o termo do meio está em OUTRA
        // tabela, e um OR que atravessa JOIN nunca vira BitmapOr. Pior, combinado com
        // "ORDER BY data_venda DESC LIMIT 20" o planner escolhia varrer o índice de data em
        // ordem e filtrar linha a linha — percorrendo a tabela inteira quando o termo era
        // seletivo, que é justamente o caso normal de uso (procurar UMA venda).
        //
        // Medido com 200 mil vendas e 50 mil clientes:
        //   termo seletivo (5 resultados):      152 ms  ->  0,63 ms
        //   termo abrangente (111k resultados): 0,08 ms ->  263 ms
        //
        // O trade-off é real e foi escolhido de olhos abertos: busca serve para achar algo
        // específico. O caso abrangente só era rápido PORQUE quase tudo casava — o plano
        // antigo encontrava 20 resultados nas primeiras linhas e parava; e um resultado de
        // 111 mil vendas não ajuda ninguém, o operador vai refinar o termo. Já o caso
        // seletivo degradava linearmente com o tamanho da tabela.
        var porNumero = noPeriodo.Where(v => EF.Functions.ILike(v.NumeroVenda, $"%{termo}%"));

        // Venda de balcão sem cadastro de cliente (cupom com nome digitado na hora, sem
        // CPF/CNPJ vinculado) — sem este ramo, buscar por nome só acharia vendas de clientes
        // cadastrados, não as de cupom avulso.
        var porCupom = noPeriodo.Where(v =>
            v.NomeCompradorCupom != null && EF.Functions.ILike(v.NomeCompradorCupom, $"%{termo}%"));

        // Subconsulta em vez de JOIN: o Postgres resolve os clientes pelo índice de trigrama
        // e entra em vendas pelo índice da FK, sem precisar juntar as duas tabelas inteiras.
        var idsClientes = context.Set<Cliente>()
            .AsNoTracking()
            .Where(c => EF.Functions.ILike(c.Nome, $"%{termo}%"))
            .Select(c => c.Id);

        var porCliente = noPeriodo.Where(v => v.ClienteId != null && idsClientes.Contains(v.ClienteId.Value));

        // Union (não Concat) para deduplicar: uma venda pode casar em mais de um ramo.
        var encontradas = porNumero.Union(porCupom).Union(porCliente);

        var total = await encontradas.CountAsync(cancellationToken);

        // Página resolvida em duas etapas: primeiro os Ids ordenados, depois o fetch com
        // Include. EF Core não permite Include depois de Union, e reidratar por lista de Ids
        // é barato (no máximo `itensPorPagina` valores).
        var idsDaPagina = await encontradas
            .OrderByDescending(v => v.DataVenda)
            .Skip((pagina - 1) * itensPorPagina)
            .Take(itensPorPagina)
            .Select(v => v.Id)
            .ToListAsync(cancellationToken);

        var itens = await context.Set<Venda>()
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Where(v => idsDaPagina.Contains(v.Id))
            .OrderByDescending(v => v.DataVenda)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    public async Task<IReadOnlyList<Venda>> ObterUltimasFinalizadasAsync(
        int quantidade = 5,
        CancellationToken cancellationToken = default)
    {
        quantidade = Math.Clamp(quantidade, 1, 50);

        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<Venda>()
            .AsNoTracking()
            .Where(v => v.Status == StatusVenda.Finalizada)
            .OrderByDescending(v => v.DataVenda)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public Task CancelarComEstornoAsync(int vendaId, CancellationToken cancellationToken = default)
        => ExecutarEmTransacaoAsync(async context =>
        {
            var venda = await context.Set<Venda>()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

            if (venda.Status == StatusVenda.Cancelada)
                throw new DomainException("Venda já está cancelada.");

            if (venda.Status == StatusVenda.Finalizada)
            {
                foreach (var item in venda.Itens)
                {
                    var (quantidadeAnterior, quantidadeAtual) = await EstoqueAtomicoHelper.ReporAsync(
                        context, item.ProdutoId, item.Quantidade, cancellationToken);

                    context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacao.Entrada,
                        Quantidade = item.Quantidade,
                        QuantidadeAnterior = quantidadeAnterior,
                        QuantidadeAtual = quantidadeAtual,
                        Motivo = $"Cancelamento venda #{venda.NumeroVenda}",
                        VendaId = vendaId
                    });
                }
            }

            venda.Status = StatusVenda.Cancelada;
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task ExcluirFisicamenteComEstornoAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        return ExecutarEmTransacaoAsync(async context =>
        {
            var possuiTrocas = await context.Set<Troca>()
                .IgnoreQueryFilters()
                .AnyAsync(t => t.VendaOrigemId == vendaId, cancellationToken);

            if (possuiTrocas)
                throw new DomainException("Não é possível excluir esta venda porque existem trocas registradas vinculadas a ela.");

            var venda = await context.Set<Venda>()
                .IgnoreQueryFilters()
                .Include(v => v.Itens)
                .FirstOrDefaultAsync(v => v.Id == vendaId, cancellationToken)
                ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

            if (venda.Status == StatusVenda.Finalizada)
            {
                foreach (var item in venda.Itens)
                {
                    var (quantidadeAnterior, quantidadeAtual) = await EstoqueAtomicoHelper.ReporAsync(
                        context, item.ProdutoId, item.Quantidade, cancellationToken);

                    context.Set<MovimentacaoEstoque>().Add(new MovimentacaoEstoque
                    {
                        ProdutoId = item.ProdutoId,
                        Tipo = TipoMovimentacao.Entrada,
                        Quantidade = item.Quantidade,
                        QuantidadeAnterior = quantidadeAnterior,
                        QuantidadeAtual = quantidadeAtual,
                        Motivo = $"Exclusão permanente venda #{venda.NumeroVenda}",
                        VendaId = vendaId
                    });
                }
            }

            context.Set<Venda>().Remove(venda);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }
}
