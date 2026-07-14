using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class VendaExternaService : IVendaExternaService
{
    private readonly IVendaExternaRepository _vendaExternaRepository;
    private readonly IProdutoRepository _produtoRepository;

    public VendaExternaService(
        IVendaExternaRepository vendaExternaRepository,
        IProdutoRepository produtoRepository)
    {
        _vendaExternaRepository = vendaExternaRepository;
        _produtoRepository = produtoRepository;
    }

    public async Task<IEnumerable<VendaExternaDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
    {
        var vendas = await _vendaExternaRepository.ObterTodosComItensAsync(cancellationToken);
        return vendas.Select(MapearParaDto);
    }

    public async Task<VendaExternaDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var venda = await _vendaExternaRepository.ObterComItensAsync(id, cancellationToken);
        return venda is null ? null : MapearParaDto(venda);
    }

    public async Task<VendaExternaDto> RegistrarAsync(RegistrarVendaExternaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Itens is null || dto.Itens.Count == 0)
            throw new DomainException("Adicione pelo menos um item à venda externa.");

        ValidarItens(dto.Itens);

        var numero = await _vendaExternaRepository.GerarNumeroVendaExternaAsync(cancellationToken);
        var venda = new VendaExterna
        {
            NumeroVendaExterna = numero,
            Observacoes = dto.Observacoes?.Trim(),
            DataVenda = DateTime.Now
        };

        var itens = dto.Itens.Select(i => new ItemVendaExterna
        {
            ProdutoId = i.ProdutoId,
            NomeProduto = i.NomeProduto.Trim(),
            CodigoBarras = string.IsNullOrWhiteSpace(i.CodigoBarras) ? null : i.CodigoBarras.Trim(),
            Quantidade = i.Quantidade,
            PrecoBase = i.PrecoBase,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        foreach (var item in itens)
            item.CalcularSubtotal();

        var registrada = await _vendaExternaRepository.RegistrarTransacionalAsync(
            venda, itens, dto.Usuario, cancellationToken);

        return MapearParaDto(registrada);
    }

    public async Task<VendaExternaDto> AtualizarAsync(AtualizarVendaExternaDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Id <= 0)
            throw new DomainException("Informe a venda externa a ser editada.");

        if (dto.Itens is null || dto.Itens.Count == 0)
            throw new DomainException("Adicione pelo menos um item à venda externa.");

        ValidarItensAtualizacao(dto.Itens);

        var itens = dto.Itens.Select(i => new ItemVendaExterna
        {
            Id = i.Id,
            ProdutoId = i.ProdutoId,
            NomeProduto = i.NomeProduto.Trim(),
            CodigoBarras = string.IsNullOrWhiteSpace(i.CodigoBarras) ? null : i.CodigoBarras.Trim(),
            Quantidade = i.Quantidade,
            PrecoBase = i.PrecoBase,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        foreach (var item in itens)
            item.CalcularSubtotal();

        var atualizada = await _vendaExternaRepository.AtualizarTransacionalAsync(
            dto.Id, dto.Observacoes, itens, dto.Usuario, cancellationToken);

        return MapearParaDto(atualizada);
    }

    public async Task ExcluirFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await _vendaExternaRepository.ObterComItensAsync(id, cancellationToken)
            ?? throw new DomainException($"Venda externa com Id {id} não encontrada.");

        await _vendaExternaRepository.ExcluirFisicamenteTransacionalAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ProcessarImportacaoTxtAsync(
        string conteudoArquivo,
        CancellationToken cancellationToken = default)
    {
        var linhas = VendaExternaTxtImportHelper.ParseArquivo(conteudoArquivo);

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha.CodigoBarras))
                continue;

            var produto = await _produtoRepository.ObterPorCodigoBarrasAsync(linha.CodigoBarras);
            if (produto is null)
                continue;

            linha.ProdutoId = produto.Id;
            linha.NomeProduto = produto.Nome;
            linha.PrecoBase = produto.PrecoVenda;
            linha.PrecoUnitario = produto.PrecoVenda;
        }

        return linhas;
    }

    private static void ValidarItens(IReadOnlyList<RegistrarItemVendaExternaDto> itens)
    {
        foreach (var item in itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
                throw new DomainException("Todos os itens devem ter um nome de produto.");

            if (item.Quantidade <= 0)
                throw new DomainException($"Quantidade inválida para '{item.NomeProduto}'.");

            if (item.PrecoUnitario < 0)
                throw new DomainException($"Preço unitário inválido para '{item.NomeProduto}'.");
        }
    }

    private static void ValidarItensAtualizacao(IReadOnlyList<AtualizarItemVendaExternaDto> itens)
    {
        foreach (var item in itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
                throw new DomainException("Todos os itens devem ter um nome de produto.");

            if (item.Quantidade <= 0)
                throw new DomainException($"Quantidade inválida para '{item.NomeProduto}'.");

            if (item.PrecoUnitario < 0)
                throw new DomainException($"Preço unitário inválido para '{item.NomeProduto}'.");
        }
    }

    private static VendaExternaDto MapearParaDto(VendaExterna venda)
        => new()
        {
            Id = venda.Id,
            NumeroVendaExterna = venda.NumeroVendaExterna,
            Subtotal = venda.Subtotal,
            Total = venda.Total,
            Observacoes = venda.Observacoes,
            Usuario = venda.Usuario,
            DataVenda = venda.DataVenda,
            Itens = venda.Itens.Select(i => new ItemVendaExternaDto
            {
                Id = i.Id,
                VendaExternaId = i.VendaExternaId,
                ProdutoId = i.ProdutoId,
                NomeProduto = i.NomeProduto,
                CodigoBarras = i.CodigoBarras,
                Quantidade = i.Quantidade,
                PrecoBase = i.PrecoBase,
                PrecoUnitario = i.PrecoUnitario,
                Subtotal = i.Subtotal
            }).ToList()
        };
}
