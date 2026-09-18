using System.Globalization;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Domain.ReadModels;
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

    public async Task<IReadOnlyList<NotaFiscal>> ListarPorVendaAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.NotasFiscais.AsNoTracking()
            .Include(n => n.Cliente)
            .Where(n => n.VendaId == vendaId)
            .OrderByDescending(n => n.DataEmissao)
            .ToListAsync(cancellationToken);
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
    /// índice único Tipo+Serie+Ambiente+Numero em <c>NotaFiscalMapping</c>.
    /// O <c>~ '^[0-9]+$'</c> protege o CAST de qualquer número não-numérico legado.
    ///
    /// O filtro por <c>ambiente</c> é obrigatório, não uma conveniência: na SEFAZ as
    /// sequências de homologação e de produção são completamente independentes (a chave de
    /// 44 dígitos nem tem dígito de ambiente — seção 8 do guia). Sem ele, uma nota de teste
    /// em homologação empurrava a numeração de produção para frente e vice-versa, abrindo
    /// buracos permanentes em ambas as séries.
    /// </summary>
    public async Task<string> ObterProximoNumeroAsync(
        TipoNotaFiscal tipo, string serie, AmbienteEmissaoFiscal ambiente, CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var codigoTipo = (int)tipo;
        var codigoAmbiente = (int)ambiente;
        var maiorNumero = await context.Database
            .SqlQuery<int>($"""
                SELECT COALESCE(MAX(CAST(numero AS INTEGER)), 0) AS "Value"
                FROM notas_fiscais
                WHERE tipo = {codigoTipo}
                  AND serie = {serie}
                  AND ambiente = {codigoAmbiente}
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

    /// <summary>
    /// Três agregações pequenas em vez de trazer notas para a memória. Diferente do
    /// dashboard de vendas (onde uma consulta ingênua materializava o mês inteiro de vendas
    /// com seus itens — ver <c>ObterResumoDiarioAsync</c>), aqui os três round-trips não são
    /// uma otimização contra um volume real: uma única loja emite dezenas de notas por mês,
    /// não centenas de milhares. Três consultas simples continuam instantâneas em qualquer
    /// volume plausível deste domínio — a divisão existe só para manter cada agregação
    /// legível (por status, por tipo, por período), não por necessidade de performance.
    ///
    /// Sem índice novo em <c>data_emissao</c> pelo mesmo motivo: um sequential scan sobre a
    /// tabela inteira de notas fiscais de uma loja é matéria de microssegundos.
    /// </summary>
    public async Task<EstatisticasNotasFiscais> ObterEstatisticasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _contextFactory.CreateDbContext();

        var porStatus = await context.NotasFiscais.AsNoTracking()
            .GroupBy(n => n.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count(), Valor = g.Sum(n => n.VNf) })
            .ToListAsync(cancellationToken);

        var porTipo = await context.NotasFiscais.AsNoTracking()
            .Where(n => n.Status == StatusNotaFiscal.Autorizada)
            .GroupBy(n => n.Tipo)
            .Select(g => new { Tipo = g.Key, Quantidade = g.Count(), Valor = g.Sum(n => n.VNf) })
            .ToListAsync(cancellationToken);

        var inicioHoje = Relogio.Hoje;
        var inicioMes = new DateTime(inicioHoje.Year, inicioHoje.Month, 1);

        var porPeriodo = await context.NotasFiscais.AsNoTracking()
            .Where(n => n.Status == StatusNotaFiscal.Autorizada)
            .GroupBy(n => 1)
            .Select(g => new
            {
                Hoje = g.Count(n => n.DataEmissao >= inicioHoje),
                ValorHoje = g.Sum(n => n.DataEmissao >= inicioHoje ? n.VNf : 0m),
                Mes = g.Count(n => n.DataEmissao >= inicioMes),
                ValorMes = g.Sum(n => n.DataEmissao >= inicioMes ? n.VNf : 0m)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var autorizadas = porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Autorizada);
        var nfe = porTipo.FirstOrDefault(t => t.Tipo == TipoNotaFiscal.NFe);
        var nfce = porTipo.FirstOrDefault(t => t.Tipo == TipoNotaFiscal.NFCe);

        return new EstatisticasNotasFiscais
        {
            TotalEmitidas = autorizadas?.Quantidade ?? 0,
            ValorTotalEmitido = autorizadas?.Valor ?? 0m,

            EmitidasHoje = porPeriodo?.Hoje ?? 0,
            ValorEmitidoHoje = porPeriodo?.ValorHoje ?? 0m,
            EmitidasNoMes = porPeriodo?.Mes ?? 0,
            ValorEmitidoNoMes = porPeriodo?.ValorMes ?? 0m,

            TotalCanceladas = porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Cancelada)?.Quantidade ?? 0,
            TotalRejeitadas = porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Rejeitada)?.Quantidade ?? 0,
            TotalDenegadas = porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Denegada)?.Quantidade ?? 0,
            TotalPendentes = (porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Rascunho)?.Quantidade ?? 0)
                           + (porStatus.FirstOrDefault(s => s.Status == StatusNotaFiscal.Indeterminada)?.Quantidade ?? 0),

            TotalNFe = nfe?.Quantidade ?? 0,
            ValorNFe = nfe?.Valor ?? 0m,
            TotalNFCe = nfce?.Quantidade ?? 0,
            ValorNFCe = nfce?.Valor ?? 0m
        };
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
