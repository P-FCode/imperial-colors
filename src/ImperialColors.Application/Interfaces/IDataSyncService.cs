namespace ImperialColors.Application.Interfaces;

public interface IDataSyncService
{
    Task<int> SincronizarPendentesAsync(CancellationToken cancellationToken = default);
}
