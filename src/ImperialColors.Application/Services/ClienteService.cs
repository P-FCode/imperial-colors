using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class ClienteService : IClienteService
{
    public const string MensagemExclusaoBloqueadaPorHistorico =
        "Este cliente não pode ser excluído permanentemente porque já possui vendas registradas no sistema.";

    private readonly IClienteRepository _clienteRepository;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(IClienteRepository clienteRepository, ILogger<ClienteService> logger)
    {
        _clienteRepository = clienteRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ClienteDto>> ObterTodosAsync()
    {
        var clientes = await _clienteRepository.ObterTodosAsync();
        return clientes.Select(MapParaDto);
    }

    public async Task<ClienteDto?> ObterPorIdAsync(int id)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id);
        return cliente is null ? null : MapParaDto(cliente);
    }

    public async Task<IEnumerable<ClienteDto>> BuscarAsync(string nome)
    {
        var clientes = string.IsNullOrWhiteSpace(nome)
            ? await _clienteRepository.ObterTodosAsync()
            : await _clienteRepository.BuscarPorNomeAsync(nome);
        return clientes.Select(MapParaDto);
    }

    public async Task<ClienteDto> CriarAsync(ClienteDto dto)
    {
        var cliente = MapParaEntidade(dto);
        var criado = await _clienteRepository.AdicionarAsync(cliente);
        _logger.LogInformation("Cliente criado: {Nome}", dto.Nome);
        return MapParaDto(criado);
    }

    public async Task<ClienteDto> AtualizarAsync(int id, ClienteDto dto)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Cliente com Id {id} não encontrado.");

        cliente.TipoPessoa = dto.TipoPessoa;
        cliente.Nome = dto.Nome;
        cliente.Cpf = dto.Cpf;
        cliente.Cnpj = dto.Cnpj;
        cliente.InscricaoEstadual = dto.InscricaoEstadual;
        cliente.Telefone = dto.Telefone;
        cliente.WhatsApp = dto.WhatsApp;
        cliente.Email = dto.Email;
        cliente.Cep = dto.Cep;
        cliente.Logradouro = dto.Logradouro;
        cliente.Numero = dto.Numero;
        cliente.Complemento = dto.Complemento;
        cliente.Bairro = dto.Bairro;
        cliente.Cidade = dto.Cidade;
        cliente.Estado = dto.Estado;
        cliente.Observacoes = dto.Observacoes;

        var atualizado = await _clienteRepository.AtualizarAsync(cliente);
        return MapParaDto(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        _ = await _clienteRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Cliente com Id {id} não encontrado.");

        if (await _clienteRepository.PossuiVinculosAsync(id))
            throw new DomainException(MensagemExclusaoBloqueadaPorHistorico);

        await _clienteRepository.RemoverFisicamenteAsync(id);

        if (await _clienteRepository.ExisteFisicamenteAsync(id))
            throw new DomainException("Não foi possível excluir o cliente. Tente novamente.");
    }

    public async Task<int> ContarAsync()
        => await _clienteRepository.ContarAsync();

    public async Task<PaginacaoResultadoDto<ClienteDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _clienteRepository.ObterPaginadoAsync(
            pagina, itensPorPagina, termoBusca, cancellationToken);

        return new PaginacaoResultadoDto<ClienteDto>
        {
            Itens = itens.Select(MapParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    private static ClienteDto MapParaDto(Cliente c) => new()
    {
        Id = c.Id, TipoPessoa = c.TipoPessoa, Nome = c.Nome, Cpf = c.Cpf,
        Cnpj = c.Cnpj, InscricaoEstadual = c.InscricaoEstadual, Telefone = c.Telefone, WhatsApp = c.WhatsApp,
        Email = c.Email, Cep = c.Cep, Logradouro = c.Logradouro, Numero = c.Numero,
        Complemento = c.Complemento, Bairro = c.Bairro, Cidade = c.Cidade,
        Estado = c.Estado, Observacoes = c.Observacoes
    };

    private static Cliente MapParaEntidade(ClienteDto dto) => new()
    {
        TipoPessoa = dto.TipoPessoa,
        Nome = InputSanitizer.SanitizarTexto(dto.Nome, 200),
        Cpf = InputSanitizer.SanitizarTexto(dto.Cpf, 14),
        Cnpj = InputSanitizer.SanitizarTexto(dto.Cnpj, 18),
        InscricaoEstadual = InputSanitizer.SanitizarTexto(dto.InscricaoEstadual, 20),
        Telefone = InputSanitizer.SanitizarTexto(dto.Telefone, 20),
        WhatsApp = InputSanitizer.SanitizarTexto(dto.WhatsApp, 20),
        Email = InputSanitizer.SanitizarEmail(dto.Email),
        Cep = InputSanitizer.SanitizarTexto(dto.Cep, 10),
        Logradouro = InputSanitizer.SanitizarTexto(dto.Logradouro, 200),
        Numero = InputSanitizer.SanitizarTexto(dto.Numero, 20),
        Complemento = InputSanitizer.SanitizarTexto(dto.Complemento, 100),
        Bairro = InputSanitizer.SanitizarTexto(dto.Bairro, 100),
        Cidade = InputSanitizer.SanitizarTexto(dto.Cidade, 100),
        Estado = InputSanitizer.SanitizarTexto(dto.Estado, 2),
        Observacoes = InputSanitizer.SanitizarTexto(dto.Observacoes, 500)
    };
}
