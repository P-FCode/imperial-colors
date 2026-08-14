using ImperialColors.Domain.Helpers;
using System.Text.Json;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Contingency;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Services;

public sealed class DataSyncService : IDataSyncService, IHostedService, IDisposable
{
    /// <summary>Quantos meses de log de auditoria são mantidos. Ver <see cref="ExpurgarLogsSeVencidoAsync"/>.</summary>
    private const int RetencaoLogsAuditoriaMeses = 12;

    /// <summary>Intervalo mínimo entre dois expurgos. O PDV costuma ficar aberto por dias
    /// seguidos, então não basta expurgar no startup — mas também não faz sentido fazer isso
    /// no ritmo de 15s do laço de sincronização.</summary>
    private static readonly TimeSpan IntervaloExpurgoLogs = TimeSpan.FromHours(24);

    private readonly IDbContextFactory<ContingencyDbContext> _contingencyFactory;
    private readonly IDbContextFactory<AppDbContext> _appFactory;
    private readonly IDatabaseHealthService _health;
    private readonly IContingencyVendaService _contingencyVenda;
    private readonly IVendaService _vendaService;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogAuditoriaRepository _logAuditoria;
    private readonly ILogger<DataSyncService> _logger;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private DateTime? _ultimoExpurgoLogs;

    /// <inheritdoc />
    public event EventHandler<int>? PendentesAlterado;

    public DataSyncService(
        IDbContextFactory<ContingencyDbContext> contingencyFactory,
        IDbContextFactory<AppDbContext> appFactory,
        IDatabaseHealthService health,
        IContingencyVendaService contingencyVenda,
        IVendaService vendaService,
        IAuditoriaService auditoria,
        ILogAuditoriaRepository logAuditoria,
        ILogger<DataSyncService> logger)
    {
        _contingencyFactory = contingencyFactory;
        _appFactory = appFactory;
        _health = health;
        _contingencyVenda = contingencyVenda;
        _vendaService = vendaService;
        _auditoria = auditoria;
        _logAuditoria = logAuditoria;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _health.StatusChanged += OnStatusChanged;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => LoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _health.StatusChanged -= OnStatusChanged;
        if (_cts is null) return;
        _cts.Cancel();
        if (_loop is not null)
        {
            try { await _loop.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { }
        }
    }

    private void OnStatusChanged(object? sender, bool online)
    {
        if (!online) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await _contingencyVenda.AtualizarCacheProdutosAsync();
                await SincronizarPendentesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar após reconexão");
            }
        });
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!_health.IsOnline) continue;
                await SincronizarPendentesAsync(cancellationToken);
                await ExpurgarLogsSeVencidoAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Retenção de <c>logs_auditoria</c>, a tabela que mais cresce do sistema (um registro por
    /// ação de operador). <c>ExpurgarAntigosAsync</c> já existia mas ninguém o agendava: rodava
    /// só uma vez, na abertura do app. Num PDV que fica aberto a semana inteira, isso
    /// significava não rodar praticamente nunca.
    ///
    /// Roda de carona no laço que já existe, no máximo uma vez por dia. O primeiro disparo
    /// acontece no primeiro tick após o app abrir — mantendo o comportamento antigo de
    /// expurgar no startup, sem precisar do bloco duplicado que havia em <c>App.xaml.cs</c>.
    ///
    /// Falha aqui nunca interrompe o laço: limpeza de histórico não pode derrubar a
    /// sincronização de vendas de contingência, que é o que realmente importa neste serviço.
    /// </summary>
    private async Task ExpurgarLogsSeVencidoAsync(CancellationToken cancellationToken)
    {
        if (_ultimoExpurgoLogs is { } ultimo && Relogio.Agora - ultimo < IntervaloExpurgoLogs)
            return;

        try
        {
            var apagados = await _logAuditoria.ExpurgarAntigosAsync(
                Relogio.Agora.AddMonths(-RetencaoLogsAuditoriaMeses), cancellationToken);

            _ultimoExpurgoLogs = Relogio.Agora;

            if (apagados > 0)
                _logger.LogInformation(
                    "Expurgo de logs de auditoria: {Quantidade} registro(s) com mais de {Meses} meses removido(s).",
                    apagados, RetencaoLogsAuditoriaMeses);
        }
        catch (Exception ex)
        {
            // Marca a tentativa mesmo em falha: sem isso, um erro persistente faria o expurgo
            // ser retentado a cada 15 segundos, enchendo o log com o próprio fracasso.
            _ultimoExpurgoLogs = Relogio.Agora;
            _logger.LogWarning(ex, "Falha ao expurgar logs de auditoria antigos.");
        }
    }

    public async Task<int> SincronizarPendentesAsync(CancellationToken cancellationToken = default)
    {
        if (!_health.IsOnline) return 0;
        if (!await _syncLock.WaitAsync(0, cancellationToken))
            return 0;

        var sincronizadas = 0;
        try
        {
            await using var local = await _contingencyFactory.CreateDbContextAsync(cancellationToken);
            var pendentes = await local.VendasContingencia
                .Include(v => v.Itens)
                .Include(v => v.Pagamentos)
                .Where(v => v.PendenteSincronizacao)
                .OrderBy(v => v.DataVenda)
                .ToListAsync(cancellationToken);

            if (pendentes.Count == 0) return 0;

            _logger.LogInformation("Sincronizando {Qtd} venda(s) de contingência...", pendentes.Count);

            foreach (var pendente in pendentes)
            {
                try
                {
                    await using var servidor = await _appFactory.CreateDbContextAsync(cancellationToken);
                    var jaExiste = await servidor.Vendas.AsNoTracking()
                        .AnyAsync(v => v.ContingenciaId == pendente.ContingenciaId, cancellationToken);

                    if (jaExiste)
                    {
                        pendente.PendenteSincronizacao = false;
                        pendente.SincronizadoEm = Relogio.Agora;
                        pendente.ErroSincronizacao = null;
                        await local.SaveChangesAsync(cancellationToken);
                        sincronizadas++;
                        continue;
                    }

                    CriarVendaDto dto;
                    try
                    {
                        dto = JsonSerializer.Deserialize<CriarVendaDto>(pendente.PayloadJson)
                              ?? throw new InvalidOperationException("Payload inválido");
                    }
                    catch
                    {
                        dto = ReconstruirDto(pendente);
                    }

                    // Evita reentrância no caminho offline — força criação online via serviço
                    var venda = await CriarVendaOnlineComContingenciaAsync(dto, pendente.ContingenciaId, cancellationToken);

                    pendente.PendenteSincronizacao = false;
                    pendente.SincronizadoEm = Relogio.Agora;
                    pendente.VendaServidorId = venda.Id;
                    pendente.ErroSincronizacao = null;
                    await local.SaveChangesAsync(cancellationToken);
                    sincronizadas++;

                    await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
                    {
                        NomeUsuario = pendente.Usuario ?? "Sistema",
                        Modulo = "PDV",
                        Acao = "SINCRONIZACAO_CONTINGENCIA",
                        Descricao = $"Venda offline {pendente.NumeroTemporario} sincronizada como {venda.NumeroVenda}",
                        Nivel = NivelLogAuditoria.Info,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            pendente.ContingenciaId,
                            VendaId = venda.Id,
                            venda.NumeroVenda
                        })
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao sincronizar contingência {Id}", pendente.ContingenciaId);
                    pendente.ErroSincronizacao = ex.Message;
                    await local.SaveChangesAsync(cancellationToken);

                    await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
                    {
                        NomeUsuario = "Sistema",
                        Modulo = "PDV",
                        Acao = "ERRO_SINCRONIZACAO_CONTINGENCIA",
                        Descricao = $"Falha ao sincronizar {pendente.NumeroTemporario}: {ex.Message}",
                        Nivel = NivelLogAuditoria.Error,
                        PayloadJson = ex.ToString()
                    }, cancellationToken);
                }
            }

            if (sincronizadas > 0)
            {
                await _contingencyVenda.AtualizarCacheProdutosAsync(cancellationToken);

                // Avisa a UI com o que RESTOU pendente (não com o que foi sincronizado): é
                // esse número que o operador precisa ver, e é ele que zera quando tudo sobe.
                // Um erro em quem escuta não pode derrubar o laço de sincronização.
                try
                {
                    PendentesAlterado?.Invoke(this, pendentes.Count - sincronizadas);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Falha ao notificar alteração de pendentes de contingência.");
                }
            }

            return sincronizadas;
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Cria a venda no PostgreSQL com ContingenciaId, reutilizando a lógica do VendaService
    /// via flag interna no DTO (Observacoes temporária) — método dedicado no serviço.
    /// </summary>
    private async Task<VendaDto> CriarVendaOnlineComContingenciaAsync(
        CriarVendaDto dto,
        Guid contingenciaId,
        CancellationToken cancellationToken)
    {
        if (_vendaService is IVendaContingenciaSync sync)
            return await sync.CriarComContingenciaIdAsync(dto, contingenciaId, cancellationToken);

        throw new InvalidOperationException("IVendaService não implementa sincronização de contingência.");
    }

    private static CriarVendaDto ReconstruirDto(VendaContingencia pendente) => new()
    {
        ClienteId = pendente.ClienteId,
        ConsumidorFinal = pendente.ConsumidorFinal,
        NomeCompradorAvulso = pendente.NomeComprador,
        DocumentoCompradorAvulso = pendente.DocumentoComprador,
        TipoPessoaCompradorAvulso = pendente.TipoPessoaComprador.HasValue
            ? (TipoPessoa)pendente.TipoPessoaComprador.Value
            : null,
        Desconto = pendente.Desconto,
        Observacoes = pendente.Observacoes,
        Usuario = pendente.Usuario,
        Itens = pendente.Itens.Select(i => new CriarItemVendaDto
        {
            ProdutoId = i.ProdutoId,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario,
            Desconto = i.Desconto
        }).ToList(),
        Pagamentos = pendente.Pagamentos.Select(p => new CriarVendaPagamentoDto
        {
            FormaPagamento = (FormaPagamento)p.FormaPagamento,
            Valor = p.Valor,
            ValorRecebido = p.ValorRecebido,
            QuantidadeParcelas = p.QuantidadeParcelas
        }).ToList()
    };

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _syncLock.Dispose();
    }
}
