namespace ImperialColors.Domain.Entities;

public class ParametroSistema
{
    public int Id { get; set; }
    public string Chave { get; set; } = string.Empty;
    public DateTime? ValorData { get; set; }
    public string? ValorTexto { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
}
