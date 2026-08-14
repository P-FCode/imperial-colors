using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class ConfiguracaoFiscalEmpresaRepository : IConfiguracaoFiscalEmpresaRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ConfiguracaoFiscalEmpresaRepository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<ConfiguracaoFiscalEmpresa?> ObterAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Set<ConfiguracaoFiscalEmpresa>()
            .AsNoTracking()
            .Include(c => c.InscricoesSubstitutoTributario)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfiguracaoFiscalEmpresa> SalvarAsync(
        ConfiguracaoFiscalEmpresa configuracao, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var existente = await context.Set<ConfiguracaoFiscalEmpresa>()
            .Include(c => c.InscricoesSubstitutoTributario)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            configuracao.AtualizadoEm = Relogio.Agora;
            await context.Set<ConfiguracaoFiscalEmpresa>().AddAsync(configuracao, cancellationToken);
        }
        else
        {
            // A entidade recém-montada pelo Service sempre chega com Id=0 (não veio do
            // banco) — sem igualar ao Id real antes do SetValues, o EF tenta "modificar"
            // a chave primária da entidade rastreada e lança InvalidOperationException
            // ("Id is part of a key and so cannot be modified").
            configuracao.Id = existente.Id;

            // Copia todos os campos escalares de uma vez (em vez de listar propriedade
            // por propriedade) — evita o tipo de bug em que um campo novo é esquecido
            // aqui e nunca persiste ao atualizar uma configuração já existente.
            context.Entry(existente).CurrentValues.SetValues(configuracao);
            existente.AtualizadoEm = Relogio.Agora;

            // Lista dinâmica (add/remove pela tela) — substitui tudo em vez de tentar
            // casar item a item, mais simples e a lista é sempre pequena (por UF).
            context.Set<InscricaoEstadualSubstituto>().RemoveRange(existente.InscricoesSubstitutoTributario);
            existente.InscricoesSubstitutoTributario = configuracao.InscricoesSubstitutoTributario
                .Select(i => new InscricaoEstadualSubstituto
                {
                    ConfiguracaoFiscalEmpresaId = existente.Id,
                    Uf = i.Uf,
                    InscricaoEstadual = i.InscricaoEstadual
                })
                .ToList();

            configuracao = existente;
        }

        await context.SaveChangesAsync(cancellationToken);
        return configuracao;
    }
}
