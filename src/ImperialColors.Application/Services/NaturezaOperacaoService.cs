using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class NaturezaOperacaoService : INaturezaOperacaoService
{
    private readonly IRepository<NaturezaOperacao> _repository;
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;
    private readonly ILogger<NaturezaOperacaoService> _logger;

    public NaturezaOperacaoService(
        IRepository<NaturezaOperacao> repository,
        IConfiguracaoFiscalService configuracaoFiscal,
        ILogger<NaturezaOperacaoService> logger)
    {
        _repository = repository;
        _configuracaoFiscal = configuracaoFiscal;
        _logger = logger;
    }

    public async Task<IEnumerable<NaturezaOperacaoDto>> ObterTodosAsync()
    {
        var itens = await _repository.ObterTodosAsync();
        return itens.OrderBy(n => n.Descricao).Select(MapParaDto);
    }

    public async Task<NaturezaOperacaoDto?> ObterPorIdAsync(int id)
    {
        var entidade = await _repository.ObterPorIdAsync(id);
        return entidade is null ? null : MapParaDto(entidade);
    }

    public async Task<NaturezaOperacaoDto> CriarAsync(NaturezaOperacaoDto dto, CancellationToken cancellationToken = default)
    {
        var regime = await _configuracaoFiscal.ObterRegimeAsync(cancellationToken);
        Sanitizar(dto);
        NaturezaOperacaoValidator.Validar(dto, regime);

        var entidade = new NaturezaOperacao();
        AplicarDto(entidade, dto);

        var criada = await _repository.AdicionarAsync(entidade);
        _logger.LogInformation("Natureza de operação criada: {Descricao} (Id={Id})", criada.Descricao, criada.Id);
        return MapParaDto(criada);
    }

    public async Task<NaturezaOperacaoDto> AtualizarAsync(NaturezaOperacaoDto dto, CancellationToken cancellationToken = default)
    {
        var existente = await _repository.ObterPorIdAsync(dto.Id)
            ?? throw new DomainException($"Natureza de operação com Id {dto.Id} não encontrada.");

        var regime = await _configuracaoFiscal.ObterRegimeAsync(cancellationToken);
        Sanitizar(dto);
        NaturezaOperacaoValidator.Validar(dto, regime);

        AplicarDto(existente, dto);
        var salva = await _repository.AtualizarAsync(existente);
        _logger.LogInformation("Natureza de operação atualizada: {Descricao} (Id={Id})", salva.Descricao, salva.Id);
        return MapParaDto(salva);
    }

    public async Task RemoverAsync(int id)
    {
        await _repository.RemoverAsync(id);
        _logger.LogInformation("Natureza de operação removida (Id={Id})", id);
    }

    private static void Sanitizar(NaturezaOperacaoDto dto)
    {
        dto.Descricao = InputSanitizer.SanitizarTexto(dto.Descricao, 150);
        dto.Serie = string.IsNullOrWhiteSpace(dto.Serie) ? null : dto.Serie.Trim();
        dto.CsosnPadrao = string.IsNullOrWhiteSpace(dto.CsosnPadrao) ? null : dto.CsosnPadrao.Trim();
        dto.CstIcmsPadrao = string.IsNullOrWhiteSpace(dto.CstIcmsPadrao) ? null : dto.CstIcmsPadrao.Trim();
        dto.CfopDentroEstado = string.IsNullOrWhiteSpace(dto.CfopDentroEstado) ? null : dto.CfopDentroEstado.Trim();
        dto.CfopForaEstado = string.IsNullOrWhiteSpace(dto.CfopForaEstado) ? null : dto.CfopForaEstado.Trim();
        dto.ObservacoesPadrao = string.IsNullOrWhiteSpace(dto.ObservacoesPadrao)
            ? null
            : InputSanitizer.SanitizarTexto(dto.ObservacoesPadrao, 500);
    }

    private static void AplicarDto(NaturezaOperacao entidade, NaturezaOperacaoDto dto)
    {
        entidade.Descricao = dto.Descricao;
        entidade.TipoOperacao = dto.TipoOperacao;
        entidade.Finalidade = dto.Finalidade;
        entidade.ConsumidorFinal = dto.ConsumidorFinal;
        entidade.Serie = dto.Serie;
        entidade.CsosnPadrao = dto.CsosnPadrao;
        entidade.CstIcmsPadrao = dto.CstIcmsPadrao;
        entidade.CfopDentroEstado = dto.CfopDentroEstado;
        entidade.CfopForaEstado = dto.CfopForaEstado;
        entidade.DifalNaoContribuinte = dto.DifalNaoContribuinte;
        entidade.ObservacoesPadrao = dto.ObservacoesPadrao;
    }

    private static NaturezaOperacaoDto MapParaDto(NaturezaOperacao n) => new()
    {
        Id = n.Id,
        Descricao = n.Descricao,
        TipoOperacao = n.TipoOperacao,
        Finalidade = n.Finalidade,
        ConsumidorFinal = n.ConsumidorFinal,
        Serie = n.Serie,
        CsosnPadrao = n.CsosnPadrao,
        CstIcmsPadrao = n.CstIcmsPadrao,
        CfopDentroEstado = n.CfopDentroEstado,
        CfopForaEstado = n.CfopForaEstado,
        DifalNaoContribuinte = n.DifalNaoContribuinte,
        ObservacoesPadrao = n.ObservacoesPadrao
    };
}
