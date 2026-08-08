using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly IRepository<Categoria> _repository;
    private readonly ITributacaoCategoriaRepository _tributacaoRepository;
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(
        IRepository<Categoria> repository,
        ITributacaoCategoriaRepository tributacaoRepository,
        IConfiguracaoFiscalService configuracaoFiscal,
        ILogger<CategoriaService> logger)
    {
        _repository = repository;
        _tributacaoRepository = tributacaoRepository;
        _configuracaoFiscal = configuracaoFiscal;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoriaDto>> ObterTodosAsync()
    {
        var categorias = await _repository.ObterTodosAsync();
        return categorias
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaDto { Id = c.Id, Nome = c.Nome, Descricao = c.Descricao });
    }

    public async Task<CategoriaDto> CriarAsync(string nome)
    {
        var nomeSanitizado = InputSanitizer.SanitizarTexto(nome, 100);
        if (string.IsNullOrWhiteSpace(nomeSanitizado))
            throw new DomainException("Nome da categoria é obrigatório.");

        var existentes = await _repository.BuscarAsync(c =>
            c.Nome.ToLower() == nomeSanitizado.ToLower());

        if (existentes.Any())
            throw new DomainException($"Já existe uma categoria com o nome '{nomeSanitizado}'.");

        var criada = await _repository.AdicionarAsync(new Categoria { Nome = nomeSanitizado });
        _logger.LogInformation("Categoria criada: {Nome} (Id={Id})", criada.Nome, criada.Id);

        return new CategoriaDto { Id = criada.Id, Nome = criada.Nome, Descricao = criada.Descricao };
    }

    public async Task<TributacaoCategoriaDto> ObterTributacaoPadraoAsync(
        int categoriaId, CancellationToken cancellationToken = default)
    {
        var tributacao = await _tributacaoRepository.ObterPorCategoriaIdAsync(categoriaId, cancellationToken);
        return tributacao is null
            ? new TributacaoCategoriaDto { CategoriaId = categoriaId }
            : MapParaDto(tributacao);
    }

    public async Task<TributacaoCategoriaDto> SalvarTributacaoPadraoAsync(
        int categoriaId, TributacaoCategoriaDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _repository.ExisteAsync(categoriaId))
            throw new DomainException($"Categoria com Id {categoriaId} não encontrada.");

        var regime = await _configuracaoFiscal.ObterRegimeAsync(cancellationToken);
        dto.CategoriaId = categoriaId;

        // Reaproveita o validador de tributação de produto: os campos compartilhados
        // (NCM, CEST, CST/CSOSN, alíquotas...) seguem exatamente as mesmas regras de
        // formato e a mesma regra CST × CSOSN conforme o regime tributário da empresa.
        TributacaoProdutoValidator.Validar(dto.ParaProdutoDto(produtoId: 0), regime);

        var entidade = new TributacaoCategoria
        {
            CategoriaId = categoriaId,
            Ncm = dto.Ncm?.Trim(),
            Cest = dto.Cest?.Trim(),
            Origem = dto.Origem,
            CstIcms = dto.CstIcms?.Trim(),
            CsosnIcms = dto.CsosnIcms?.Trim(),
            AliquotaIcms = dto.AliquotaIcms,
            AliquotaIcmsSt = dto.AliquotaIcmsSt,
            Mva = dto.Mva,
            ReducaoBaseCalculo = dto.ReducaoBaseCalculo,
            CstPis = dto.CstPis?.Trim(),
            AliquotaPis = dto.AliquotaPis,
            CstCofins = dto.CstCofins?.Trim(),
            AliquotaCofins = dto.AliquotaCofins,
            CstIpi = dto.CstIpi?.Trim(),
            CodigoEnquadramentoIpi = dto.CodigoEnquadramentoIpi?.Trim(),
            AliquotaIpi = dto.AliquotaIpi,
            CfopDentroEstado = dto.CfopDentroEstado?.Trim(),
            CfopForaEstado = dto.CfopForaEstado?.Trim(),
            CstIbsCbs = dto.CstIbsCbs?.Trim(),
            CClassTrib = dto.CClassTrib?.Trim(),
            CstIS = dto.CstIS?.Trim(),
            CClassTribIS = dto.CClassTribIS?.Trim(),
            AliquotaIS = dto.AliquotaIS,
            AliquotaIbsMunicipioDiferimento = dto.AliquotaIbsMunicipioDiferimento,
            AliquotaIbsMunicipioReducao = dto.AliquotaIbsMunicipioReducao
        };

        var salvo = await _tributacaoRepository.SalvarAsync(entidade, cancellationToken);
        _logger.LogInformation("Perfil fiscal padrão salvo para a categoria Id={CategoriaId}", categoriaId);
        return MapParaDto(salvo);
    }

    private static TributacaoCategoriaDto MapParaDto(TributacaoCategoria t) => new()
    {
        CategoriaId = t.CategoriaId,
        Ncm = t.Ncm,
        Cest = t.Cest,
        Origem = t.Origem,
        CstIcms = t.CstIcms,
        CsosnIcms = t.CsosnIcms,
        AliquotaIcms = t.AliquotaIcms,
        AliquotaIcmsSt = t.AliquotaIcmsSt,
        Mva = t.Mva,
        ReducaoBaseCalculo = t.ReducaoBaseCalculo,
        CstPis = t.CstPis,
        AliquotaPis = t.AliquotaPis,
        CstCofins = t.CstCofins,
        AliquotaCofins = t.AliquotaCofins,
        CstIpi = t.CstIpi,
        CodigoEnquadramentoIpi = t.CodigoEnquadramentoIpi,
        AliquotaIpi = t.AliquotaIpi,
        CfopDentroEstado = t.CfopDentroEstado,
        CfopForaEstado = t.CfopForaEstado,
        CstIbsCbs = t.CstIbsCbs,
        CClassTrib = t.CClassTrib,
        CstIS = t.CstIS,
        CClassTribIS = t.CClassTribIS,
        AliquotaIS = t.AliquotaIS,
        AliquotaIbsMunicipioDiferimento = t.AliquotaIbsMunicipioDiferimento,
        AliquotaIbsMunicipioReducao = t.AliquotaIbsMunicipioReducao
    };
}
