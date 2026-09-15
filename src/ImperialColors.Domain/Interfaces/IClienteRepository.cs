using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    /// <summary>Compara só os dígitos: cadastros antigos podem ter o documento com ou sem máscara.</summary>
    Task<Cliente?> ObterPorDocumentoAsync(
        string documentoSomenteDigitos, TipoPessoa tipoPessoa, int? ignorarClienteId = null, CancellationToken cancellationToken = default);

    Task<IEnumerable<Cliente>> BuscarPorNomeAsync(string nome);
    Task<Cliente?> ObterComVendasAsync(int id);
    Task<(IReadOnlyList<Cliente> Itens, int Total)> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);
    Task<bool> PossuiVinculosAsync(int clienteId, CancellationToken cancellationToken = default);
    Task RemoverFisicamenteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExisteFisicamenteAsync(int id, CancellationToken cancellationToken = default);
}
