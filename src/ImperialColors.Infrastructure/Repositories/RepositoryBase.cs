using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ImperialColors.Infrastructure.Repositories;

public class RepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    protected readonly IDbContextFactory<AppDbContext> ContextFactory;
    private readonly ILogger? _logger;

    public RepositoryBase(IDbContextFactory<AppDbContext> contextFactory, ILogger? logger = null)
    {
        ContextFactory = contextFactory;
        _logger = logger;
    }

    public virtual async Task<T?> ObterPorIdAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// ⚠️ Carrega a tabela INTEIRA, sem paginação nem teto. Use apenas em tabelas de domínio
    /// fechado, que não crescem com o uso: categorias, marcas, naturezas de operação,
    /// fornecedores. Para qualquer tabela que acompanhe o movimento da loja (vendas, itens,
    /// movimentações, notas, logs) existe uma variante paginada — e <c>Venda</c> deixou de
    /// sobrescrever este método justamente para que ninguém o chame lá por engano.
    /// </summary>
    public virtual async Task<IEnumerable<T>> ObterTodosAsync()
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> BuscarAsync(Expression<Func<T, bool>> predicate)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
    }

    public virtual async Task<T> AdicionarAsync(T entity)
    {
        await using var context = ContextFactory.CreateDbContext();
        await context.Set<T>().AddAsync(entity);
        await SalvarAlteracoesAsync(context);
        return entity;
    }

    public virtual async Task<T> AtualizarAsync(T entity)
    {
        await using var context = ContextFactory.CreateDbContext();
        context.Set<T>().Update(entity);
        await SalvarAlteracoesAsync(context);
        return entity;
    }

    public virtual async Task RemoverAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        var entity = await context.Set<T>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entity is null || !entity.Ativo)
            return;

        entity.Ativo = false;
        entity.AtualizadoEm = Relogio.Agora;
        await SalvarAlteracoesAsync(context);
    }

    public virtual async Task<bool> ExisteAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<T>().AnyAsync(e => e.Id == id);
    }

    public virtual async Task<int> ContarAsync(Expression<Func<T, bool>>? predicate = null)
    {
        await using var context = ContextFactory.CreateDbContext();
        var query = context.Set<T>().AsQueryable();
        return predicate is null
            ? await query.CountAsync()
            : await query.CountAsync(predicate);
    }

    /// <summary>
    /// Executa <paramref name="operacao"/> dentro de uma transação, sob a estratégia de
    /// execução do provider — a forma correta de combinar transação manual com
    /// <c>EnableRetryOnFailure</c>.
    ///
    /// Sem este envelope, o EF lança <c>InvalidOperationException</c> ("does not support
    /// user-initiated transactions") no primeiro <c>SaveChanges</c> dentro de um
    /// <c>BeginTransaction</c> manual sempre que houver estratégia de retry ativa. O detalhe
    /// traiçoeiro: o <c>BeginTransaction</c> sozinho NÃO lança, só o ciclo completo com
    /// escrita — um teste que apenas abre e fecha transação passaria, e o erro só apareceria
    /// na primeira venda real do cliente.
    ///
    /// Dois cuidados que fazem o retry ser correto e não só compilável:
    ///
    /// 1. O <see cref="AppDbContext"/> é criado DENTRO do lambda. A estratégia reexecuta o
    ///    delegate inteiro; reaproveitar um contexto de fora traria o change tracker sujo da
    ///    tentativa anterior, com as entidades já marcadas como Added e chaves preenchidas.
    ///
    /// 2. Tudo que a operação precisa escrever tem que ser feito por dentro dela. Entidades
    ///    recebidas de fora (ex.: a <c>Venda</c> montada pelo serviço) são reanexadas a cada
    ///    tentativa; como as colunas de Id são <c>IDENTITY ALWAYS</c>, o Postgres gera chave
    ///    nova a cada inserção e o valor obsoleto da tentativa anterior é sobrescrito.
    ///
    /// Sem retry configurado, a estratégia padrão simplesmente executa uma vez — então este
    /// envelope é neutro hoje e continua correto se a política mudar.
    /// </summary>
    /// <param name="operacao">Recebe o contexto da tentativa. Não recebe CancellationToken de
    /// propósito: o delegate captura o token do método que o criou, o que mantém o corpo das
    /// operações idêntico ao que era antes deste envelope existir.</param>
    protected async Task<TResultado> ExecutarEmTransacaoAsync<TResultado>(
        Func<AppDbContext, Task<TResultado>> operacao,
        CancellationToken cancellationToken)
    {
        // A estratégia vem do provider, não do contexto em si; um contexto efêmero só para
        // obtê-la é barato (com o factory pooled, nem chega a abrir conexão).
        await using var contextoParaEstrategia = ContextFactory.CreateDbContext();
        var estrategia = contextoParaEstrategia.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var context = ContextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var resultado = await operacao(context);
                await transaction.CommitAsync(cancellationToken);
                return resultado;
            }
            catch
            {
                // Rollback é BEST-EFFORT, e isso não é zelo excessivo: quando a falha é a
                // própria queda da conexão (o caso que o retry existe para tratar), o
                // RollbackAsync lança ObjectDisposedException. Sem este try/catch, essa
                // exceção secundária SUBSTITUI a original — a estratégia deixa de reconhecer
                // um erro transitório, não repete, e o operador recebe "ObjectDisposedException:
                // NpgsqlTransaction" em vez de qualquer coisa acionável.
                //
                // Perder o rollback aqui é inofensivo: se a conexão caiu, o servidor já
                // desfez a transação sozinho.
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch
                {
                    // Intencionalmente engolido — ver acima. O `throw` abaixo repropaga a
                    // exceção ORIGINAL, que é a que interessa.
                }

                throw;
            }
        });
    }

    /// <summary>Sobrecarga para operações sem valor de retorno.</summary>
    protected Task ExecutarEmTransacaoAsync(
        Func<AppDbContext, Task> operacao,
        CancellationToken cancellationToken)
        => ExecutarEmTransacaoAsync<object?>(async contexto =>
        {
            await operacao(contexto);
            return null;
        }, cancellationToken);

    protected static void Desanexar(AppDbContext context, object entity)
    {
        var entry = context.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    protected async Task SalvarAlteracoesAsync(AppDbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var detalhe = DatabaseExceptionHelper.ObterMensagemDetalhada(ex);
            _logger?.LogError(ex, "Erro ao persistir {Entidade}: {Detalhe}", typeof(T).Name, detalhe);
            throw new DomainException($"Erro real do banco: {detalhe}", ex);
        }
    }
}
