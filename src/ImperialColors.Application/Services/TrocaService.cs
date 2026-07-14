using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class TrocaService : ITrocaService
{
    private readonly ITrocaRepository _trocaRepository;
    private readonly IVendaRepository _vendaRepository;
    private readonly IVendaExternaRepository _vendaExternaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ILogger<TrocaService> _logger;

    public TrocaService(
        ITrocaRepository trocaRepository,
        IVendaRepository vendaRepository,
        IVendaExternaRepository vendaExternaRepository,
        IProdutoRepository produtoRepository,
        ILogger<TrocaService> logger)
    {
        _trocaRepository = trocaRepository;
        _vendaRepository = vendaRepository;
        _vendaExternaRepository = vendaExternaRepository;
        _produtoRepository = produtoRepository;
        _logger = logger;
    }

    public async Task<TrocaDto> RegistrarAsync(RegistrarTrocaDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.QuantidadeDevolvida <= 0)
            throw new DomainException("Quantidade devolvida deve ser maior que zero.");
        if (dto.QuantidadeNova <= 0)
            throw new DomainException("Quantidade do novo item deve ser maior que zero.");
        if (dto.PrecoUnitarioNovo < 0)
            throw new DomainException("Preço do novo item não pode ser negativo.");

        var venda = await _vendaRepository.ObterComItensAsync(dto.VendaOrigemId)
            ?? throw new DomainException($"Venda #{dto.VendaOrigemId} não encontrada.");

        if (venda.Status != StatusVenda.Finalizada)
            throw new DomainException("Somente vendas finalizadas podem ter itens trocados.");

        var itensOrdenados = BinarySearchCollectionHelper.OrdenarPorId(venda.Itens, i => i.Id);
        var itemOrigem = BinarySearchCollectionHelper.FindById(itensOrdenados, dto.ItemVendaOrigemId, i => i.Id)
            ?? throw new DomainException("Item selecionado não pertence à venda informada.");

        if (dto.QuantidadeDevolvida > itemOrigem.Quantidade)
            throw new DomainException($"Quantidade devolvida ({dto.QuantidadeDevolvida}) não pode ser maior que a quantidade da venda ({itemOrigem.Quantidade}).");

        var produtoDevolvido = await _produtoRepository.ObterPorIdAsync(itemOrigem.ProdutoId)
            ?? throw new DomainException($"Produto devolvido (Id={itemOrigem.ProdutoId}) não encontrado.");

        var produtoNovo = await _produtoRepository.ObterPorIdAsync(dto.ProdutoNovoId)
            ?? throw new DomainException($"Novo produto (Id={dto.ProdutoNovoId}) não encontrado.");

        if (produtoNovo.QuantidadeEstoque < dto.QuantidadeNova)
            throw new DomainException($"Estoque insuficiente para '{produtoNovo.Nome}'. Disponível: {produtoNovo.QuantidadeEstoque} {produtoNovo.Unidade}.");

        var troca = new Troca
        {
            VendaOrigemId = dto.VendaOrigemId,
            ProdutoDevolvidoId = itemOrigem.ProdutoId,
            QuantidadeDevolvida = dto.QuantidadeDevolvida,
            ValorUnitarioDevolucao = itemOrigem.PrecoUnitario,
            RetornarAoEstoque = dto.RetornarAoEstoque,
            ProdutoNovoId = dto.ProdutoNovoId,
            QuantidadeNova = dto.QuantidadeNova,
            ValorUnitarioNovo = dto.PrecoUnitarioNovo,
            FormaPagamentoDiferenca = dto.FormaPagamentoDiferenca,
            Observacoes = dto.Observacoes,
            Usuario = dto.Usuario,
            DataTroca = DateTime.UtcNow
        };

        await _trocaRepository.RegistrarTrocaTransacionalAsync(
            troca, produtoDevolvido, produtoNovo, dto.RetornarAoEstoque, cancellationToken);

        _logger.LogInformation(
            "Troca registrada: Venda#{VendaId}, Devolveu ProdId={ProdDevId} x{QtdDev}, Levou ProdId={ProdNovoId} x{QtdNova}, Diferença={Diferenca}",
            dto.VendaOrigemId, itemOrigem.ProdutoId, dto.QuantidadeDevolvida,
            dto.ProdutoNovoId, dto.QuantidadeNova, troca.DiferencaValor);

        return MapParaDto(troca, venda.NumeroVenda, produtoDevolvido.Nome, produtoNovo.Nome);
    }

    public async Task<TrocaDto> RegistrarVendaExternaAsync(
        RegistrarTrocaVendaExternaDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.QuantidadeDevolvida <= 0)
            throw new DomainException("Quantidade devolvida deve ser maior que zero.");
        if (dto.QuantidadeNova <= 0)
            throw new DomainException("Quantidade do novo item deve ser maior que zero.");
        if (dto.PrecoUnitarioNovo < 0)
            throw new DomainException("Preço do novo item não pode ser negativo.");

        var venda = await _vendaExternaRepository.ObterComItensAsync(dto.VendaExternaOrigemId, cancellationToken)
            ?? throw new DomainException($"Venda externa #{dto.VendaExternaOrigemId} não encontrada.");

        var itemOrigem = venda.Itens.FirstOrDefault(i => i.Id == dto.ItemVendaExternaOrigemId)
            ?? throw new DomainException("Item selecionado não pertence à venda externa informada.");

        if (!itemOrigem.ProdutoId.HasValue)
            throw new DomainException("Somente itens vinculados ao estoque podem ser trocados.");

        if (dto.QuantidadeDevolvida > itemOrigem.Quantidade)
            throw new DomainException(
                $"Quantidade devolvida ({dto.QuantidadeDevolvida}) não pode ser maior que a quantidade da venda ({itemOrigem.Quantidade}).");

        var produtoDevolvido = await _produtoRepository.ObterPorIdAsync(itemOrigem.ProdutoId.Value)
            ?? throw new DomainException($"Produto devolvido (Id={itemOrigem.ProdutoId}) não encontrado.");

        var produtoNovo = await _produtoRepository.ObterPorIdAsync(dto.ProdutoNovoId)
            ?? throw new DomainException($"Novo produto (Id={dto.ProdutoNovoId}) não encontrado.");

        if (produtoNovo.QuantidadeEstoque < dto.QuantidadeNova)
            throw new DomainException(
                $"Estoque insuficiente para '{produtoNovo.Nome}'. Disponível: {produtoNovo.QuantidadeEstoque} {produtoNovo.Unidade}.");

        var troca = new Troca
        {
            VendaExternaOrigemId = dto.VendaExternaOrigemId,
            ProdutoDevolvidoId = itemOrigem.ProdutoId.Value,
            QuantidadeDevolvida = dto.QuantidadeDevolvida,
            ValorUnitarioDevolucao = itemOrigem.PrecoUnitario,
            RetornarAoEstoque = dto.RetornarAoEstoque,
            ProdutoNovoId = dto.ProdutoNovoId,
            QuantidadeNova = dto.QuantidadeNova,
            ValorUnitarioNovo = dto.PrecoUnitarioNovo,
            FormaPagamentoDiferenca = dto.FormaPagamentoDiferenca,
            Observacoes = dto.Observacoes,
            Usuario = dto.Usuario,
            DataTroca = DateTime.UtcNow
        };

        await _trocaRepository.RegistrarTrocaVendaExternaTransacionalAsync(
            troca, produtoDevolvido, produtoNovo, dto.RetornarAoEstoque, cancellationToken);

        _logger.LogInformation(
            "Troca venda externa registrada: VendaExt#{VendaId}, Devolveu ProdId={ProdDevId} x{QtdDev}, Levou ProdId={ProdNovoId} x{QtdNova}",
            dto.VendaExternaOrigemId, itemOrigem.ProdutoId, dto.QuantidadeDevolvida,
            dto.ProdutoNovoId, dto.QuantidadeNova);

        return MapParaDto(troca, venda.NumeroVendaExterna, produtoDevolvido.Nome, produtoNovo.Nome);
    }

    public async Task<IEnumerable<TrocaDto>> ObterPorVendaAsync(int vendaId)
    {
        var trocas = await _trocaRepository.ObterPorVendaAsync(vendaId);
        return trocas.Select(t => MapParaDto(t,
            t.VendaOrigem?.NumeroVenda ?? $"#{t.VendaOrigemId ?? t.VendaExternaOrigemId}",
            t.ProdutoDevolvido?.Nome ?? $"Produto #{t.ProdutoDevolvidoId}",
            t.ProdutoNovo?.Nome ?? $"Produto #{t.ProdutoNovoId}"));
    }

    private static TrocaDto MapParaDto(Troca t, string numVenda, string nomeDev, string nomeNovo) => new()
    {
        Id = t.Id,
        VendaOrigemId = t.VendaOrigemId,
        VendaExternaOrigemId = t.VendaExternaOrigemId,
        NumeroVendaOrigem = numVenda,
        ProdutoDevolvidoId = t.ProdutoDevolvidoId,
        ProdutoDevolvidoNome = nomeDev,
        QuantidadeDevolvida = t.QuantidadeDevolvida,
        ValorUnitarioDevolucao = t.ValorUnitarioDevolucao,
        RetornarAoEstoque = t.RetornarAoEstoque,
        ProdutoNovoId = t.ProdutoNovoId,
        ProdutoNovoNome = nomeNovo,
        QuantidadeNova = t.QuantidadeNova,
        ValorUnitarioNovo = t.ValorUnitarioNovo,
        FormaPagamentoDiferenca = t.FormaPagamentoDiferenca,
        FormaPagamentoDiferencaDescricao = t.FormaPagamentoDiferenca?.ToString(),
        ValorTotalDevolvido = t.ValorTotalDevolvido,
        ValorTotalNovo = t.ValorTotalNovo,
        DiferencaValor = t.DiferencaValor,
        Observacoes = t.Observacoes,
        Usuario = t.Usuario,
        DataTroca = t.DataTroca
    };
}
