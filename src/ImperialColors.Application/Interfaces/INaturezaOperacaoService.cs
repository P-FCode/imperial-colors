using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>CRUD das Naturezas de Operação — regras fiscais reutilizáveis por tipo de venda.</summary>
public interface INaturezaOperacaoService
{
    Task<IEnumerable<NaturezaOperacaoDto>> ObterTodosAsync();
    Task<NaturezaOperacaoDto?> ObterPorIdAsync(int id);
    Task<NaturezaOperacaoDto> CriarAsync(NaturezaOperacaoDto dto, CancellationToken cancellationToken = default);
    Task<NaturezaOperacaoDto> AtualizarAsync(NaturezaOperacaoDto dto, CancellationToken cancellationToken = default);
    Task RemoverAsync(int id);
}
