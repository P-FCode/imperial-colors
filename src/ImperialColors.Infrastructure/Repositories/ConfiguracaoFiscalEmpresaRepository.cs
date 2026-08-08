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
            configuracao.AtualizadoEm = DateTime.UtcNow;
            await context.Set<ConfiguracaoFiscalEmpresa>().AddAsync(configuracao, cancellationToken);
        }
        else
        {
            existente.IeIsenta = configuracao.IeIsenta;
            existente.InscricaoMunicipal = configuracao.InscricaoMunicipal;
            existente.InscricaoSuframa = configuracao.InscricaoSuframa;
            existente.Cnae = configuracao.Cnae;
            existente.DifalNaoContribuinte = configuracao.DifalNaoContribuinte;
            existente.DifalStContribuinte = configuracao.DifalStContribuinte;
            existente.Cep = configuracao.Cep;
            existente.Logradouro = configuracao.Logradouro;
            existente.Numero = configuracao.Numero;
            existente.Complemento = configuracao.Complemento;
            existente.Bairro = configuracao.Bairro;
            existente.CodigoMunicipioIbge = configuracao.CodigoMunicipioIbge;
            existente.NomeMunicipio = configuracao.NomeMunicipio;
            existente.Uf = configuracao.Uf;
            existente.Serie = configuracao.Serie;
            existente.Ambiente = configuracao.Ambiente;
            existente.IdCscHomologacao = configuracao.IdCscHomologacao;
            existente.CscHomologacao = configuracao.CscHomologacao;
            existente.IdCscProducao = configuracao.IdCscProducao;
            existente.CscProducao = configuracao.CscProducao;
            existente.SimplesExcessoSublimite = configuracao.SimplesExcessoSublimite;
            existente.AliquotaIbsUfPadrao = configuracao.AliquotaIbsUfPadrao;
            existente.AliquotaIbsMunicipioPadrao = configuracao.AliquotaIbsMunicipioPadrao;
            existente.AliquotaCbsPadrao = configuracao.AliquotaCbsPadrao;
            existente.CstIbsCbsPadrao = configuracao.CstIbsCbsPadrao;
            existente.CClassTribPadrao = configuracao.CClassTribPadrao;
            existente.ValidarNcmEmNotas = configuracao.ValidarNcmEmNotas;
            existente.BloquearEdicaoNumeroNota = configuracao.BloquearEdicaoNumeroNota;
            existente.BloquearNotaComItensMenorQueVenda = configuracao.BloquearNotaComItensMenorQueVenda;
            existente.FretePorContaPadrao = configuracao.FretePorContaPadrao;
            existente.EmailPadraoEnvioNotas = configuracao.EmailPadraoEnvioNotas;
            existente.IndicadorPresencaPadrao = configuracao.IndicadorPresencaPadrao;
            existente.GerarNotaAutomaticaAoFinalizarVenda = configuracao.GerarNotaAutomaticaAoFinalizarVenda;
            existente.CancelarNotaAutomaticoAoCancelarVenda = configuracao.CancelarNotaAutomaticoAoCancelarVenda;
            existente.AtualizadoEm = DateTime.UtcNow;

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
