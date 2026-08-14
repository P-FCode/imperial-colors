using ImperialColors.Domain.Helpers;

namespace ImperialColors.Domain.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; } = Relogio.Agora;
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;
}
