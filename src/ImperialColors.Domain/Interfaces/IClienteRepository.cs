using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<IEnumerable<Cliente>> BuscarPorNomeAsync(string nome);
    Task<Cliente?> ObterComVendasAsync(int id);
    Task<(IReadOnlyList<Cliente> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);
    Task<bool> PossuiVinculosAsync(int clienteId, CancellationToken cancellationToken = default);
    Task RemoverFisicamenteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteFisicamenteAsync(int id, CancellationToken cancellationToken = default);
}
