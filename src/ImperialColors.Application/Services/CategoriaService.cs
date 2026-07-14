using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly IRepository<Categoria> _repository;
    private readonly ILogger<CategoriaService> _logger;

    public CategoriaService(IRepository<Categoria> repository, ILogger<CategoriaService> logger)
    {
        _repository = repository;
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
}
