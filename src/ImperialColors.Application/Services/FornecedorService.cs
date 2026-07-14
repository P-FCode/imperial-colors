using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class FornecedorService : IFornecedorService
{
    public const string MensagemExclusaoBloqueadaPorHistorico =
        "Este fornecedor não pode ser excluído permanentemente porque possui produtos ou listas de compra vinculados.";

    private readonly IFornecedorRepository _fornecedorRepository;
    private readonly ILogger<FornecedorService> _logger;

    public FornecedorService(IFornecedorRepository fornecedorRepository, ILogger<FornecedorService> logger)
    {
        _fornecedorRepository = fornecedorRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<FornecedorDto>> ObterTodosAsync()
    {
        var fornecedores = await _fornecedorRepository.ObterTodosAsync();
        return fornecedores.Select(MapParaDto);
    }

    public async Task<FornecedorDto?> ObterPorIdAsync(int id)
    {
        var fornecedor = await _fornecedorRepository.ObterPorIdAsync(id);
        return fornecedor is null ? null : MapParaDto(fornecedor);
    }

    public async Task<IEnumerable<FornecedorDto>> BuscarAsync(string nome)
    {
        var fornecedores = string.IsNullOrWhiteSpace(nome)
            ? await _fornecedorRepository.ObterTodosAsync()
            : await _fornecedorRepository.BuscarPorNomeAsync(nome);
        return fornecedores.Select(MapParaDto);
    }

    public async Task<FornecedorDto> CriarAsync(FornecedorDto dto)
    {
        dto.TipoPessoa = Domain.Enums.TipoPessoa.Juridica;
        var fornecedor = MapParaEntidade(dto);
        var criado = await _fornecedorRepository.AdicionarAsync(fornecedor);
        return MapParaDto(criado);
    }

    public async Task<FornecedorDto> AtualizarAsync(int id, FornecedorDto dto)
    {
        var fornecedor = await _fornecedorRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Fornecedor com Id {id} não encontrado.");

        fornecedor.TipoPessoa = Domain.Enums.TipoPessoa.Juridica;
        fornecedor.Nome = dto.Nome;
        fornecedor.Cnpj = dto.Cnpj;
        fornecedor.InscricaoEstadual = dto.InscricaoEstadual;
        fornecedor.Telefone = dto.Telefone;
        fornecedor.WhatsApp = dto.WhatsApp;
        fornecedor.Email = dto.Email;
        fornecedor.Cep = dto.Cep;
        fornecedor.Logradouro = dto.Logradouro;
        fornecedor.Numero = dto.Numero;
        fornecedor.Complemento = dto.Complemento;
        fornecedor.Bairro = dto.Bairro;
        fornecedor.Cidade = dto.Cidade;
        fornecedor.Estado = dto.Estado;
        fornecedor.Observacoes = dto.Observacoes;

        var atualizado = await _fornecedorRepository.AtualizarAsync(fornecedor);
        return MapParaDto(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        _ = await _fornecedorRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Fornecedor com Id {id} não encontrado.");

        if (await _fornecedorRepository.PossuiVinculosAsync(id))
            throw new DomainException(MensagemExclusaoBloqueadaPorHistorico);

        await _fornecedorRepository.RemoverFisicamenteAsync(id);

        if (await _fornecedorRepository.ExisteFisicamenteAsync(id))
            throw new DomainException("Não foi possível excluir o fornecedor. Tente novamente.");
    }

    public async Task<PaginacaoResultadoDto<FornecedorDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _fornecedorRepository.ObterPaginadoAsync(
            pagina, itensPorPagina, termoBusca, cancellationToken);

        return new PaginacaoResultadoDto<FornecedorDto>
        {
            Itens = itens.Select(MapParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    private static FornecedorDto MapParaDto(Fornecedor f) => new()
    {
        Id = f.Id, TipoPessoa = f.TipoPessoa, Nome = f.Nome, Cnpj = f.Cnpj,
        InscricaoEstadual = f.InscricaoEstadual, Telefone = f.Telefone, WhatsApp = f.WhatsApp,
        Email = f.Email, Cep = f.Cep, Logradouro = f.Logradouro, Numero = f.Numero,
        Complemento = f.Complemento, Bairro = f.Bairro, Cidade = f.Cidade,
        Estado = f.Estado, Observacoes = f.Observacoes
    };

    private static Fornecedor MapParaEntidade(FornecedorDto dto) => new()
    {
        TipoPessoa = Domain.Enums.TipoPessoa.Juridica,
        Nome = dto.Nome, Cnpj = dto.Cnpj, InscricaoEstadual = dto.InscricaoEstadual,
        Telefone = dto.Telefone, WhatsApp = dto.WhatsApp,
        Email = dto.Email, Cep = dto.Cep, Logradouro = dto.Logradouro, Numero = dto.Numero,
        Complemento = dto.Complemento, Bairro = dto.Bairro, Cidade = dto.Cidade,
        Estado = dto.Estado, Observacoes = dto.Observacoes
    };
}
