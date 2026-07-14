using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IParametroSistemaRepository
{
    Task<DateTime?> ObterDataAsync(string chave, CancellationToken cancellationToken = default);
    Task SalvarDataAsync(string chave, DateTime valor, CancellationToken cancellationToken = default);
}
