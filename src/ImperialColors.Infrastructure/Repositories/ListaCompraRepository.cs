using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class ListaCompraRepository : RepositoryBase<ListaCompra>, IListaCompraRepository
{
    public ListaCompraRepository(IDbContextFactory<AppDbContext> contextFactory) : base(contextFactory) { }

    public async Task<IEnumerable<ListaCompra>> ObterTodosComItensAsync(CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<ListaCompra>()
            .AsNoTracking()
            .Include(l => l.Fornecedor)
            .Include(l => l.Itens).ThenInclude(i => i.Produto)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => new ListaCompra
            {
                Id = l.Id,
                Nome = l.Nome,
                FornecedorId = l.FornecedorId,
                Finalizada = l.Finalizada,
                Observacoes = l.Observacoes,
                NotaFiscalNomeArquivo = l.NotaFiscalNomeArquivo,
                CriadoEm = l.CriadoEm,
                AtualizadoEm = l.AtualizadoEm,
                Ativo = l.Ativo,
                Fornecedor = l.Fornecedor,
                Itens = l.Itens.ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ListaCompra?> ObterComItensAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        return await context.Set<ListaCompra>()
            .AsNoTracking()
            .Include(l => l.Fornecedor)
            .Include(l => l.Itens).ThenInclude(i => i.Produto)
            .Where(l => l.Id == id)
            .Select(l => new ListaCompra
            {
                Id = l.Id,
                Nome = l.Nome,
                FornecedorId = l.FornecedorId,
                Finalizada = l.Finalizada,
                Observacoes = l.Observacoes,
                NotaFiscalNomeArquivo = l.NotaFiscalNomeArquivo,
                CriadoEm = l.CriadoEm,
                AtualizadoEm = l.AtualizadoEm,
                Ativo = l.Ativo,
                Fornecedor = l.Fornecedor,
                Itens = l.Itens.ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ListaCompra> SalvarComItensAsync(
        ListaCompra lista,
        IReadOnlyList<ItemListaCompra> itens,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            ListaCompra entidade;

            if (lista.Id > 0)
            {
                entidade = await context.Set<ListaCompra>()
                    .Include(l => l.Itens)
                    .FirstOrDefaultAsync(l => l.Id == lista.Id, cancellationToken)
                    ?? throw new InvalidOperationException($"Lista de compra com Id {lista.Id} não encontrada.");

                entidade.Nome = lista.Nome;
                entidade.FornecedorId = lista.FornecedorId;
                entidade.Finalizada = lista.Finalizada;
                entidade.Observacoes = lista.Observacoes;

                context.Set<ItemListaCompra>().RemoveRange(entidade.Itens);
            }
            else
            {
                entidade = new ListaCompra
                {
                    Nome = lista.Nome,
                    FornecedorId = lista.FornecedorId,
                    Finalizada = lista.Finalizada,
                    Observacoes = lista.Observacoes
                };
                await context.Set<ListaCompra>().AddAsync(entidade, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            foreach (var item in itens)
            {
                context.Set<ItemListaCompra>().Add(new ItemListaCompra
                {
                    ListaCompraId = entidade.Id,
                    ProdutoId = item.ProdutoId,
                    DescricaoItem = item.DescricaoItem,
                    QuantidadeDesejada = item.QuantidadeDesejada,
                    QuantidadeComprada = item.QuantidadeComprada,
                    Comprado = item.Comprado,
                    Observacoes = item.Observacoes
                });
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return (await ObterComItensAsync(entidade.Id, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public override async Task RemoverAsync(int id)
    {
        await using var context = ContextFactory.CreateDbContext();
        var lista = await context.Set<ListaCompra>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lista is null)
            return;

        context.Set<ListaCompra>().Remove(lista);
        await SalvarAlteracoesAsync(context);
    }

    public override async Task<IEnumerable<ListaCompra>> ObterTodosAsync()
        => await ObterTodosComItensAsync();

    public async Task AnexarNotaFiscalAsync(
        int id,
        byte[] conteudo,
        string nomeArquivo,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var lista = await context.Set<ListaCompra>()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Lista de compra com Id {id} não encontrada.");

        lista.NotaFiscalConteudo = conteudo;
        lista.NotaFiscalNomeArquivo = nomeArquivo;
        lista.AtualizadoEm = DateTime.UtcNow;
        await SalvarAlteracoesAsync(context);
    }

    public async Task<(byte[] Conteudo, string NomeArquivo)?> ObterNotaFiscalAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var context = ContextFactory.CreateDbContext();
        var nota = await context.Set<ListaCompra>()
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new { l.NotaFiscalConteudo, l.NotaFiscalNomeArquivo })
            .FirstOrDefaultAsync(cancellationToken);

        if (nota?.NotaFiscalConteudo is null || string.IsNullOrWhiteSpace(nota.NotaFiscalNomeArquivo))
            return null;

        return (nota.NotaFiscalConteudo, nota.NotaFiscalNomeArquivo);
    }
}