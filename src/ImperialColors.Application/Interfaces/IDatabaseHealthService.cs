namespace ImperialColors.Application.Interfaces;

public interface IDatabaseHealthService
{
    bool IsOnline { get; }
    event EventHandler<bool>? StatusChanged;
    Task<bool> VerificarAgoraAsync(CancellationToken cancellationToken = default);
    void MarcarOffline();
}
