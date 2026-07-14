using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class ListaCompraService : IListaCompraService
{
    private readonly IListaCompraRepository _listaCompraRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ILogger<ListaCompraService> _logger;

    public ListaCompraService(
        IListaCompraRepository listaCompraRepository,
        IProdutoRepository produtoRepository,
        ILogger<ListaCompraService> logger)
    {
        _listaCompraRepository = listaCompraRepository;
        _produtoRepository = produtoRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ListaCompraDto>> ObterTodosAsync(
        string? termoBusca = null,
        CancellationToken cancellationToken = default)
    {
        var listas = await _listaCompraRepository.ObterTodosComItensAsync(cancellationToken);
        var resultado = listas.Select(MapParaDto);

        if (string.IsNullOrWhiteSpace(termoBusca))
            return resultado;

        var termo = termoBusca.Trim();
        return resultado.Where(l =>
            l.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
            (l.FornecedorNome?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false));
    }

    public async Task<ListaCompraDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var lista = await _listaCompraRepository.ObterComItensAsync(id, cancellationToken);
        return lista is null ? null : MapParaDto(lista);
    }

    public async Task<ListaCompraDto> SalvarAsync(SalvarListaCompraDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new DomainException("Informe o nome da lista de compras.");

        if (dto.Itens.Count == 0)
            throw new DomainException("Adicione pelo menos um item à lista.");

        foreach (var item in dto.Itens)
        {
            if (item.QuantidadeDesejada <= 0)
                throw new DomainException("A quantidade desejada deve ser maior que zero.");

            if (item.ProdutoId.HasValue && item.ProdutoId > 0)
            {
                _ = await _produtoRepository.ObterPorIdAsync(item.ProdutoId.Value)
                    ?? throw new DomainException($"Produto com Id {item.ProdutoId} não encontrado.");
            }
            else if (string.IsNullOrWhiteSpace(item.NomeManual))
            {
                throw new DomainException("Informe o produto do estoque ou a descrição manual do item.");
            }
        }

        var lista = new ListaCompra
        {
            Id = dto.Id,
            Nome = dto.Nome.Trim(),
            FornecedorId = dto.FornecedorId,
            Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim()
        };

        var itens = dto.Itens.Select(i => new ItemListaCompra
        {
            ProdutoId = i.ProdutoId is > 0 ? i.ProdutoId : null,
            DescricaoItem = i.ProdutoId is > 0 || string.IsNullOrWhiteSpace(i.NomeManual)
                ? null
                : i.NomeManual.Trim(),
            QuantidadeDesejada = i.QuantidadeDesejada,
            QuantidadeComprada = i.QuantidadeComprada,
            Comprado = i.Comprado,
            Observacoes = string.IsNullOrWhiteSpace(i.Observacoes) ? null : i.Observacoes.Trim()
        }).ToList();

        var salva = await _listaCompraRepository.SalvarComItensAsync(lista, itens, cancellationToken);
        _logger.LogInformation("Lista de compra salva: {Nome} ({TotalItens} itens)", salva.Nome, salva.Itens.Count);
        return MapParaDto(salva);
    }

    public async Task RemoverAsync(int id)
    {
        if (!await _listaCompraRepository.ExisteAsync(id))
            throw new DomainException($"Lista de compra com Id {id} não encontrada.");

        await _listaCompraRepository.RemoverAsync(id);
        _logger.LogInformation("Lista de compra removida: {Id}", id);
    }

    public async Task AlterarFinalizadaAsync(int id, bool finalizada)
    {
        var lista = await _listaCompraRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Lista de compra com Id {id} não encontrada.");

        lista.Finalizada = finalizada;
        await _listaCompraRepository.AtualizarAsync(lista);
    }

    public async Task AnexarNotaFiscalAsync(
        int id,
        byte[] conteudo,
        string nomeArquivo,
        CancellationToken cancellationToken = default)
    {
        if (conteudo.Length == 0)
            throw new DomainException("O arquivo selecionado está vazio.");

        const int tamanhoMaximoBytes = 15 * 1024 * 1024;
        if (conteudo.Length > tamanhoMaximoBytes)
            throw new DomainException("O arquivo excede o limite de 15 MB.");

        var extensao = Path.GetExtension(nomeArquivo).ToLowerInvariant();
        if (extensao is not (".pdf" or ".png" or ".jpg" or ".jpeg"))
            throw new DomainException("Formato não suportado. Use PDF, PNG ou JPEG.");

        _ = await _listaCompraRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Lista de compra com Id {id} não encontrada.");

        await _listaCompraRepository.AnexarNotaFiscalAsync(id, conteudo, nomeArquivo, cancellationToken);
        _logger.LogInformation("Nota fiscal anexada à lista {Id}: {Arquivo}", id, nomeArquivo);
    }

    public async Task<NotaFiscalListaCompraDto?> ObterNotaFiscalAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var nota = await _listaCompraRepository.ObterNotaFiscalAsync(id, cancellationToken);
        if (nota is null)
            return null;

        return new NotaFiscalListaCompraDto
        {
            Conteudo = nota.Value.Conteudo,
            NomeArquivo = nota.Value.NomeArquivo
        };
    }

    private static ListaCompraDto MapParaDto(ListaCompra lista)
    {
        var dto = new ListaCompraDto
        {
            Id = lista.Id,
            Nome = lista.Nome,
            FornecedorId = lista.FornecedorId,
            FornecedorNome = lista.Fornecedor?.Nome,
            Finalizada = lista.Finalizada,
            Observacoes = lista.Observacoes,
            CriadoEm = lista.CriadoEm,
            PossuiNotaFiscal = !string.IsNullOrWhiteSpace(lista.NotaFiscalNomeArquivo),
            NotaFiscalNomeArquivo = lista.NotaFiscalNomeArquivo,
            Itens = lista.Itens.Select(i => new ItemListaCompraDto
            {
                Id = i.Id,
                ListaCompraId = i.ListaCompraId,
                ProdutoId = i.ProdutoId,
                NomeProduto = i.Produto?.Nome ?? i.DescricaoItem ?? string.Empty,
                QuantidadeDesejada = i.QuantidadeDesejada,
                QuantidadeComprada = i.QuantidadeComprada,
                Comprado = i.Comprado,
                Observacoes = i.Observacoes,
                Unidade = i.Produto?.Unidade ?? "UN"
            }).ToList()
        };

        return dto;
    }
}
