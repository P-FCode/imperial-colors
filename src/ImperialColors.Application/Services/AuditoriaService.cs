using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly ILogAuditoriaRepository _repository;
    private readonly ILogger<AuditoriaService> _logger;

    public AuditoriaService(ILogAuditoriaRepository repository, ILogger<AuditoriaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RegistrarAsync(RegistrarLogAuditoriaDto dto, CancellationToken cancellationToken = default)
    {
        var log = new LogAuditoria
        {
            DataHora = DateTime.UtcNow,
            UsuarioId = dto.UsuarioId,
            NomeUsuario = string.IsNullOrWhiteSpace(dto.NomeUsuario) ? "Sistema" : dto.NomeUsuario.Trim(),
            Modulo = string.IsNullOrWhiteSpace(dto.Modulo) ? "Sistema" : dto.Modulo.Trim(),
            Acao = dto.Acao?.Trim() ?? string.Empty,
            Descricao = dto.Descricao?.Trim() ?? string.Empty,
            Nivel = dto.Nivel,
            PayloadJson = dto.PayloadJson
        };

        try
        {
            await _repository.AdicionarAsync(log, cancellationToken);
        }
        catch (Exception ex)
        {
            // Auditoria nunca deve derrubar o fluxo principal
            _logger.LogWarning(ex, "Falha ao gravar log de auditoria ({Modulo}/{Acao})", log.Modulo, log.Acao);
        }
    }

    public async Task<PaginacaoResultadoDto<LogAuditoriaDto>> ObterPaginadoAsync(
        FiltroLogAuditoriaDto filtro,
        CancellationToken cancellationToken = default)
    {
        var pagina = Math.Max(1, filtro.Pagina);
        var itensPorPagina = Math.Clamp(filtro.ItensPorPagina, 10, 200);

        var (itens, total) = await _repository.ObterPaginadoAsync(
            filtro.DataInicio,
            filtro.DataFim,
            filtro.Nivel,
            filtro.Modulo,
            filtro.TermoBusca,
            pagina,
            itensPorPagina,
            cancellationToken);

        return new PaginacaoResultadoDto<LogAuditoriaDto>
        {
            Itens = itens.Select(Mapear).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    public async Task<LogAuditoriaDto?> ObterPorIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _repository.ObterPorIdAsync(id, cancellationToken);
        return log is null ? null : Mapear(log);
    }

    private static LogAuditoriaDto Mapear(LogAuditoria log) => new()
    {
        Id = log.Id,
        DataHora = log.DataHora,
        UsuarioId = log.UsuarioId,
        NomeUsuario = log.NomeUsuario,
        Modulo = log.Modulo,
        Acao = log.Acao,
        Descricao = log.Descricao,
        Nivel = log.Nivel,
        PayloadJson = log.PayloadJson
    };
}
