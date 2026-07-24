using ImperialColors.Application.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Services;

public sealed class DatabaseHealthService : IDatabaseHealthService, IHostedService, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<DatabaseHealthService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromSeconds(8);
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private bool _isOnline = true;

    public DatabaseHealthService(
        IDbContextFactory<AppDbContext> factory,
        ILogger<DatabaseHealthService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public bool IsOnline => _isOnline;

    public event EventHandler<bool>? StatusChanged;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loop = Task.Run(() => ExecutarLoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null) return;
        _cts.Cancel();
        if (_loop is not null)
        {
            try { await _loop.WaitAsync(cancellationToken); }
            catch (OperationCanceledException) { }
        }
    }

    public async Task<bool> VerificarAgoraAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
            var ok = await ctx.Database.CanConnectAsync(cancellationToken);
            AtualizarStatus(ok);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha no health-check do PostgreSQL");
            AtualizarStatus(false);
            return false;
        }
    }

    public void MarcarOffline() => AtualizarStatus(false);

    private async Task ExecutarLoopAsync(CancellationToken cancellationToken)
    {
        await VerificarAgoraAsync(cancellationToken);

        using var timer = new PeriodicTimer(_intervalo);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await VerificarAgoraAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // encerramento normal
        }
    }

    private void AtualizarStatus(bool online)
    {
        if (_isOnline == online) return;
        _isOnline = online;
        _logger.LogInformation("Status do banco: {Status}", online ? "Online" : "Offline (Contingência)");
        StatusChanged?.Invoke(this, online);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
