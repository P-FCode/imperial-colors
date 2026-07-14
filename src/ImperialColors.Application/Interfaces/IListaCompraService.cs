using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IListaCompraService
{
    Task<IEnumerable<ListaCompraDto>> ObterTodosAsync(string? termoBusca = null, CancellationToken cancellationToken = default);
    Task<ListaCompraDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ListaCompraDto> SalvarAsync(SalvarListaCompraDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(int id);
    Task AlterarFinalizadaAsync(int id, bool finalizada);
    Task AnexarNotaFiscalAsync(int id, byte[] conteudo, string nomeArquivo, CancellationToken cancellationToken = default);
    Task<NotaFiscalListaCompraDto?> ObterNotaFiscalAsync(int id, CancellationToken cancellationToken = default);
}
