using System.Globalization;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public NotaFiscalRepository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<NotaFiscal?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await Consulta(context)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<NotaFiscal?> ObterPorChaveAcessoAsync(string chaveAcesso, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await Consulta(context)
            .FirstOrDefaultAsync(n => n.ChaveAcesso == chaveAcesso, cancellationToken);
    }

    public async Task<IReadOnlyList<NotaFiscal>> ListarAsync(
        TipoNotaFiscal tipo, StatusNotaFiscal? status = null, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context.NotasFiscais.AsNoTracking()
            .Include(n => n.Cliente)
            .Where(n => n.Tipo == tipo);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        return await query.OrderByDescending(n => n.DataEmissao).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Próximo número livre da série, resolvido com um <c>MAX</c> no banco.
    ///
    /// A versão anterior trazia TODOS os números da série para o cliente
    /// (<c>Select(n =&gt; n.Numero).ToListAsync()</c>) e calculava o <c>Max()</c> em memória —
    /// com 100 mil notas, cada rascunho criado e cada reemissão transferia 100 mil strings
    /// antes de montar o payload. O motivo de não ser um <c>MAX</c> direto é que
    /// <c>numero</c> é <c>varchar(9)</c> sem zero à esquerda, então a ordenação de texto não
    /// corresponde à numérica ("9" &gt; "12"); o <c>CAST</c> resolve isso, e o índice de
    /// expressão criado em <c>AddIndiceNumeracaoNotaFiscal</c> faz o Postgres responder por
    /// leitura de uma única entrada de índice.
    ///
    /// Sem filtro de soft-delete de propósito (SQL cru não aplica query filter, e é o que se
    /// quer aqui): a numeração é imutável mesmo para notas inativadas ou rejeitadas — ver o
    /// índice único Tipo+Serie+Numero em <c>NotaFiscalMapping</c>.
    /// O <c>~ '^[0-9]+$'</c> protege o CAST de qualquer número não-numérico legado.
    /// </summary>
    public async Task<string> ObterProximoNumeroAsync(TipoNotaFiscal tipo, string serie, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var codigoTipo = (int)tipo;
        var maiorNumero = await context.Database
            .SqlQuery<int>($"""
                SELECT COALESCE(MAX(CAST(numero AS INTEGER)), 0) AS "Value"
                FROM notas_fiscais
                WHERE tipo = {codigoTipo}
                  AND serie = {serie}
                  AND numero ~ '^[0-9]+$'
                """)
            .SingleAsync(cancellationToken);

        return (maiorNumero + 1).ToString(CultureInfo.InvariantCulture);
    }

    public async Task<NotaFiscal> CriarAsync(NotaFiscal nota, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.NotasFiscais.AddAsync(nota, cancellationToken);
        await SalvarAlteracoesAsync(context, cancellationToken);
        return nota;
    }

    public async Task AtualizarAsync(NotaFiscal nota, bool substituirItens = false, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var existente = await context.NotasFiscais
            .Include(n => n.Itens)
            .Include(n => n.Pagamentos)
            .FirstOrDefaultAsync(n => n.Id == nota.Id, cancellationToken);

        if (existente is null)
            throw new DomainException($"Nota fiscal com Id {nota.Id} não encontrada.");

        context.Entry(existente).CurrentValues.SetValues(nota);

        if (substituirItens)
        {
            context.ItensNotaFiscal.RemoveRange(existente.Itens);
            context.NotaFiscalPagamentos.RemoveRange(existente.Pagamentos);

            foreach (var item in nota.Itens)
            {
                item.Id = 0;
                item.NotaFiscalId = existente.Id;
            }
            foreach (var pagamento in nota.Pagamentos)
            {
                pagamento.Id = 0;
                pagamento.NotaFiscalId = existente.Id;
            }

            await context.ItensNotaFiscal.AddRangeAsync(nota.Itens, cancellationToken);
            await context.NotaFiscalPagamentos.AddRangeAsync(nota.Pagamentos, cancellationToken);
        }

        existente.AtualizadoEm = Relogio.Agora;
        await SalvarAlteracoesAsync(context, cancellationToken);
    }

    public async Task<NotaFiscalEvento> AdicionarEventoAsync(NotaFiscalEvento evento, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.NotaFiscalEventos.AddAsync(evento, cancellationToken);
        await SalvarAlteracoesAsync(context, cancellationToken);
        return evento;
    }

    public async Task ExcluirAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        var nota = await context.NotasFiscais.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
        if (nota is null || !nota.Ativo)
            return;

        nota.Ativo = false;
        nota.AtualizadoEm = Relogio.Agora;
        await SalvarAlteracoesAsync(context, cancellationToken);
    }

    public async Task<(int Emitidas, int Canceladas, decimal ValorTotalEmitido)> ObterContadoresAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var emitidas = await context.NotasFiscais.AsNoTracking()
            .CountAsync(n => n.Status == StatusNotaFiscal.Autorizada, cancellationToken);
        var canceladas = await context.NotasFiscais.AsNoTracking()
            .CountAsync(n => n.Status == StatusNotaFiscal.Cancelada, cancellationToken);
        var valorTotalEmitido = await context.NotasFiscais.AsNoTracking()
            .Where(n => n.Status == StatusNotaFiscal.Autorizada)
            .SumAsync(n => n.VNf, cancellationToken);

        return (emitidas, canceladas, valorTotalEmitido);
    }

    public async Task<IReadOnlyList<NotaFiscal>> ListarUltimasAsync(int quantidade, CancellationToken cancellationToken = default)
    {
        quantidade = Math.Clamp(quantidade, 1, 50);

        await using var context = _contextFactory.CreateDbContext();
        return await context.NotasFiscais.AsNoTracking()
            .Include(n => n.Cliente)
            .OrderByDescending(n => n.DataEmissao)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<NotaFiscal> Consulta(AppDbContext context) =>
        context.NotasFiscais
            .Include(n => n.Itens)
            .Include(n => n.Pagamentos)
            .Include(n => n.Eventos.OrderByDescending(e => e.DataHora))
            .Include(n => n.Cliente)
            .Include(n => n.NaturezaOperacao);

    private static async Task SalvarAlteracoesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // ObterProximoNumeroAsync não usa lock — em uma corrida real entre duas emissões
            // concorrentes, a constraint única (Tipo, Serie, Numero) do banco é a última linha
            // de defesa. Traduzir para uma mensagem acionável em vez do erro cru do Postgres.
            if (DatabaseExceptionHelper.EhViolacaoUnicidadeNumeracaoNotaFiscal(ex))
                throw new DomainException(
                    "Esse número já foi usado nesta série — outra nota foi salva com o mesmo número enquanto esta tela estava aberta. Atualize o número e tente novamente.", ex);

            throw new DomainException($"Erro ao salvar nota fiscal: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }
}
