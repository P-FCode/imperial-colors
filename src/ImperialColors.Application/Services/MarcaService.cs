using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class MarcaService : IMarcaService
{
    private readonly IRepository<Marca> _repository;
    private readonly ILogger<MarcaService> _logger;

    public MarcaService(IRepository<Marca> repository, ILogger<MarcaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<MarcaDto>> ObterTodosAsync()
    {
        var marcas = await _repository.ObterTodosAsync();
        return marcas
            .OrderBy(m => m.Nome)
            .Select(m => new MarcaDto { Id = m.Id, Nome = m.Nome, Descricao = m.Descricao });
    }

    public async Task<MarcaDto> CriarAsync(string nome)
    {
        var nomeSanitizado = InputSanitizer.SanitizarTexto(nome, 100);
        if (string.IsNullOrWhiteSpace(nomeSanitizado))
            throw new DomainException("Nome da marca é obrigatório.");

        var existentes = await _repository.BuscarAsync(m =>
            m.Nome.ToLower() == nomeSanitizado.ToLower());

        if (existentes.Any())
            throw new DomainException($"Já existe uma marca com o nome '{nomeSanitizado}'.");

        var criada = await _repository.AdicionarAsync(new Marca { Nome = nomeSanitizado });
        _logger.LogInformation("Marca criada: {Nome} (Id={Id})", criada.Nome, criada.Id);

        return new MarcaDto { Id = criada.Id, Nome = criada.Nome, Descricao = criada.Descricao };
    }
}
