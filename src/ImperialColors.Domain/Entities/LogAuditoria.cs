using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;

namespace ImperialColors.Domain.Entities;

/// <summary>
/// Registro imutável de auditoria operacional. Não herda BaseEntity
/// (sem soft-delete) e usa Id long para volume alto de eventos.
/// </summary>
public class LogAuditoria
{
    public long Id { get; set; }
    public DateTime DataHora { get; set; } = Relogio.Agora;
    public int? UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public NivelLogAuditoria Nivel { get; set; } = NivelLogAuditoria.Info;
    public string? PayloadJson { get; set; }
}
