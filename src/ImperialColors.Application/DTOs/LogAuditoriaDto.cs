using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.DTOs;

public class LogAuditoriaDto
{
    public long Id { get; set; }
    public DateTime DataHora { get; set; }
    public int? UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Acao { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public NivelLogAuditoria Nivel { get; set; }
    public string NivelDescricao => Nivel switch
    {
        NivelLogAuditoria.Info => "Informação",
        NivelLogAuditoria.Warning => "Alerta",
        NivelLogAuditoria.Error => "Erro",
        NivelLogAuditoria.Critical => "Crítico",
        _ => Nivel.ToString()
    };
    public string? PayloadJson { get; set; }
}

public class FiltroLogAuditoriaDto
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public NivelLogAuditoria? Nivel { get; set; }
    public string? Modulo { get; set; }
    public string? TermoBusca { get; set; }
    public int Pagina { get; set; } = 1;
    public int ItensPorPagina { get; set; } = 50;
}

public class RegistrarLogAuditoriaDto
{
    public int? UsuarioId { get; set; }
    public string NomeUsuario { get; set; } = "Sistema";
    public string Modulo { get; set; } = "Sistema";
    public string Acao { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public NivelLogAuditoria Nivel { get; set; } = NivelLogAuditoria.Info;
    public string? PayloadJson { get; set; }
}
