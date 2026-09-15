namespace ImperialColors.Application.DTOs;

/// <summary>Venda offline que tentou subir para o servidor e foi recusada.</summary>
public class PendenciaSincronizacaoDto
{
    public string NumeroTemporario { get; set; } = string.Empty;
    public DateTime DataVenda { get; set; }
    public decimal Total { get; set; }
    public string Erro { get; set; } = string.Empty;
}
