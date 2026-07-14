namespace ImperialColors.Application.DTOs;

public class PaginacaoResultadoDto<T>
{
    public IReadOnlyList<T> Itens { get; init; } = Array.Empty<T>();
    public int PaginaAtual { get; init; }
    public int ItensPorPagina { get; init; }
    public int TotalItens { get; init; }
    public int TotalPaginas => ItensPorPagina <= 0
        ? 0
        : (int)Math.Ceiling(TotalItens / (double)ItensPorPagina);
}
