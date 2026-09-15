using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class ClienteService : IClienteService
{
    public const string MensagemExclusaoBloqueadaPorHistorico =
        "Este cliente não pode ser excluído permanentemente porque já possui vendas registradas no sistema.";

    private readonly IClienteRepository _clienteRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ILogger<ClienteService> _logger;

    public ClienteService(
        IClienteRepository clienteRepository,
        IAuditoriaService auditoria,
        IUsuarioAtual usuarioAtual,
        ILogger<ClienteService> logger)
    {
        _clienteRepository = clienteRepository;
        _auditoria = auditoria;
        _usuarioAtual = usuarioAtual;
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
        ClienteValidator.Validar(dto);
        await GarantirDocumentoUnicoAsync(dto, ignorarClienteId: null);
        var cliente = new Cliente();
        AplicarDados(cliente, dto);
        var criado = await _clienteRepository.AdicionarAsync(cliente);
        _logger.LogInformation("Cliente criado: {Nome}", dto.Nome);
        return MapParaDto(criado);
    }

    public async Task<ClienteDto> AtualizarAsync(int id, ClienteDto dto)
    {
        ClienteValidator.Validar(dto);

        var cliente = await _clienteRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Cliente com Id {id} não encontrado.");

        // Só confere quando o documento muda: duplicados antigos continuam editáveis (telefone, endereço...).
        var documentoAlterado = cliente.TipoPessoa != dto.TipoPessoa
            || SomenteDigitos(dto.TipoPessoa == TipoPessoa.Juridica ? cliente.Cnpj : cliente.Cpf)
               != SomenteDigitos(dto.TipoPessoa == TipoPessoa.Juridica ? dto.Cnpj : dto.Cpf);
        if (documentoAlterado)
            await GarantirDocumentoUnicoAsync(dto, ignorarClienteId: id);

        AplicarDados(cliente, dto);

        var atualizado = await _clienteRepository.AtualizarAsync(cliente);
        return MapParaDto(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Cliente com Id {id} não encontrado.");

        if (await _clienteRepository.PossuiVinculosAsync(id))
            throw new DomainException(MensagemExclusaoBloqueadaPorHistorico);

        await _clienteRepository.RemoverFisicamenteAsync(id);

        if (await _clienteRepository.ExisteFisicamenteAsync(id))
            throw new DomainException("Não foi possível excluir o cliente. Tente novamente.");

        await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = _usuarioAtual.Nome,
            Modulo = "Clientes",
            Acao = "CLIENTE_EXCLUIDO",
            Descricao = $"Cliente '{cliente.Nome}' (Id {id}) excluído permanentemente",
            Nivel = NivelLogAuditoria.Warning,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                cliente.Id, cliente.Nome, cliente.TipoPessoa, cliente.Cpf, cliente.Cnpj, cliente.Telefone, cliente.Email
            })
        });
    }

    private async Task GarantirDocumentoUnicoAsync(ClienteDto dto, int? ignorarClienteId)
    {
        var juridica = dto.TipoPessoa == TipoPessoa.Juridica;
        var digitos = SomenteDigitos(juridica ? dto.Cnpj : dto.Cpf);
        if (digitos.Length == 0)
            return;

        var existente = await _clienteRepository.ObterPorDocumentoAsync(digitos, dto.TipoPessoa, ignorarClienteId);
        if (existente is not null)
            throw new DomainException(
                $"Já existe um cliente cadastrado com este {(juridica ? "CNPJ" : "CPF")}: {existente.Nome}.");
    }

    private static string SomenteDigitos(string? valor)
        => new(valor?.Where(char.IsDigit).ToArray() ?? []);

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
        Estado = c.Estado, CodigoMunicipioIbge = c.CodigoMunicipioIbge, IndicadorIe = c.IndicadorIe,
        Observacoes = c.Observacoes
    };

    // Criação e edição passam por aqui; os limites são os das colunas em ClienteMapping.
    private static void AplicarDados(Cliente cliente, ClienteDto dto)
    {
        cliente.TipoPessoa = dto.TipoPessoa;
        cliente.Nome = InputSanitizer.SanitizarTexto(dto.Nome, 200);
        cliente.Cpf = InputSanitizer.SanitizarTexto(dto.Cpf, 14);
        cliente.Cnpj = InputSanitizer.SanitizarTexto(dto.Cnpj, 18);
        cliente.InscricaoEstadual = InputSanitizer.SanitizarTexto(dto.InscricaoEstadual, 20);
        cliente.Telefone = InputSanitizer.SanitizarTexto(dto.Telefone, 20);
        cliente.WhatsApp = InputSanitizer.SanitizarTexto(dto.WhatsApp, 20);
        cliente.Email = LimitarTamanho(InputSanitizer.SanitizarEmail(dto.Email), 200);
        cliente.Cep = InputSanitizer.SanitizarTexto(dto.Cep, 10);
        cliente.Logradouro = InputSanitizer.SanitizarTexto(dto.Logradouro, 200);
        cliente.Numero = InputSanitizer.SanitizarTexto(dto.Numero, 10);
        cliente.Complemento = InputSanitizer.SanitizarTexto(dto.Complemento, 100);
        cliente.Bairro = InputSanitizer.SanitizarTexto(dto.Bairro, 100);
        cliente.Cidade = InputSanitizer.SanitizarTexto(dto.Cidade, 100);
        cliente.Estado = InputSanitizer.SanitizarTexto(dto.Estado, 2);
        cliente.CodigoMunicipioIbge = InputSanitizer.SanitizarTexto(dto.CodigoMunicipioIbge, 7);
        cliente.IndicadorIe = dto.IndicadorIe;
        cliente.Observacoes = InputSanitizer.SanitizarTexto(dto.Observacoes, 500);
    }

    private static string LimitarTamanho(string valor, int maximo)
        => valor.Length > maximo ? valor[..maximo] : valor;
}
