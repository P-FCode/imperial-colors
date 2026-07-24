using System.Text.Json;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Contingency;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Services;

public class ContingencyVendaService : IContingencyVendaService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IDbContextFactory<ContingencyDbContext> _factory;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ILogger<ContingencyVendaService> _logger;

    public ContingencyVendaService(
        IDbContextFactory<ContingencyDbContext> factory,
        IProdutoRepository produtoRepository,
        ILogger<ContingencyVendaService> logger)
    {
        _factory = factory;
        _produtoRepository = produtoRepository;
        _logger = logger;
    }

    public async Task<VendaDto> SalvarVendaOfflineAsync(CriarVendaDto dto, CancellationToken cancellationToken = default)
    {
        if (!dto.Itens.Any())
            throw new DomainException("A venda deve ter pelo menos um item.");

        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);

        foreach (var item in dto.Itens)
        {
            var cache = await ctx.EstoqueLocal.FindAsync([item.ProdutoId], cancellationToken);
            if (cache is not null && cache.QuantidadeEstoque < item.Quantidade)
            {
                throw new DomainException(
                    $"Estoque local insuficiente para '{cache.Nome}'. Disponível: {cache.QuantidadeEstoque}");
            }
        }

        var contingenciaId = Guid.NewGuid();
        var numeroTemp = $"OFF-{DateTime.Now:yyyyMMdd}-{contingenciaId.ToString("N")[..8].ToUpperInvariant()}";

        var itensTemp = new List<(CriarItemVendaDto Dto, string Nome, decimal Subtotal)>();
        decimal subtotal = 0;
        foreach (var item in dto.Itens)
        {
            var cache = await ctx.EstoqueLocal.FindAsync([item.ProdutoId], cancellationToken);
            var nome = cache?.Nome ?? $"Produto #{item.ProdutoId}";
            var itemSub = (item.Quantidade * item.PrecoUnitario) - item.Desconto;
            subtotal += itemSub;
            itensTemp.Add((item, nome, itemSub));
        }

        var total = subtotal - dto.Desconto;
        var pagamentos = PagamentoHelper.NormalizarPagamentos(dto, total);
        PagamentoHelper.ValidarPagamentosCompostos(total, pagamentos);

        var payload = JsonSerializer.Serialize(dto, JsonOptions);
        var venda = new VendaContingencia
        {
            ContingenciaId = contingenciaId,
            NumeroTemporario = numeroTemp,
            DataVenda = DateTime.Now,
            ClienteId = dto.ClienteId,
            ConsumidorFinal = dto.ConsumidorFinal,
            NomeComprador = dto.NomeCompradorAvulso,
            DocumentoComprador = dto.DocumentoCompradorAvulso,
            TipoPessoaComprador = dto.TipoPessoaCompradorAvulso.HasValue
                ? (int)dto.TipoPessoaCompradorAvulso.Value
                : null,
            Subtotal = subtotal,
            Desconto = dto.Desconto,
            Total = total,
            Observacoes = dto.Observacoes,
            Usuario = dto.Usuario,
            PayloadJson = payload,
            PendenteSincronizacao = true,
            Itens = itensTemp.Select(i => new ItemVendaContingencia
            {
                ProdutoId = i.Dto.ProdutoId,
                NomeProduto = i.Nome,
                Quantidade = i.Dto.Quantidade,
                PrecoUnitario = i.Dto.PrecoUnitario,
                Desconto = i.Dto.Desconto,
                Subtotal = i.Subtotal
            }).ToList(),
            Pagamentos = pagamentos.Select((p, idx) => new PagamentoContingencia
            {
                FormaPagamento = (int)p.FormaPagamento,
                Valor = p.Valor,
                ValorRecebido = p.ValorRecebido,
                QuantidadeParcelas = p.QuantidadeParcelas,
                Ordem = idx + 1
            }).ToList()
        };

        ctx.VendasContingencia.Add(venda);

        foreach (var item in dto.Itens)
        {
            var cache = await ctx.EstoqueLocal.FindAsync([item.ProdutoId], cancellationToken);
            if (cache is not null)
            {
                cache.QuantidadeEstoque -= item.Quantidade;
                cache.AtualizadoEm = DateTime.UtcNow;
            }
        }

        await ctx.SaveChangesAsync(cancellationToken);
        _logger.LogWarning(
            "Venda salva em contingência offline: {Numero} ({ContingenciaId})",
            numeroTemp, contingenciaId);

        var (formaResumo, parcelasResumo, valorPagoResumo, trocoTotal) =
            PagamentoHelper.ResumirPagamentosLegado(pagamentos);

        return new VendaDto
        {
            Id = 0,
            NumeroVenda = numeroTemp,
            ClienteId = dto.ClienteId,
            NomeCompradorCupom = dto.ConsumidorFinal
                ? "Consumidor Final"
                : dto.NomeCompradorAvulso,
            DocumentoCompradorCupom = dto.DocumentoCompradorAvulso,
            TipoPessoaComprador = dto.TipoPessoaCompradorAvulso,
            Status = StatusVenda.Finalizada,
            Subtotal = subtotal,
            Desconto = dto.Desconto,
            Total = total,
            FormaPagamento = pagamentos.Count > 1 ? formaResumo : pagamentos[0].FormaPagamento,
            QuantidadeParcelas = parcelasResumo,
            ValorPago = valorPagoResumo,
            Troco = trocoTotal,
            Observacoes = $"[OFFLINE] {dto.Observacoes}".Trim(),
            Usuario = dto.Usuario,
            DataVenda = venda.DataVenda,
            Pagamentos = pagamentos.Select((p, i) => new VendaPagamentoDto
            {
                FormaPagamento = p.FormaPagamento,
                Valor = p.Valor,
                ValorRecebido = p.ValorRecebido,
                QuantidadeParcelas = p.QuantidadeParcelas,
                Ordem = i + 1
            }).ToList(),
            Itens = itensTemp.Select(i => new ItemVendaDto
            {
                ProdutoId = i.Dto.ProdutoId,
                NomeProduto = i.Nome,
                Quantidade = i.Dto.Quantidade,
                PrecoUnitario = i.Dto.PrecoUnitario,
                Desconto = i.Dto.Desconto,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }

    public async Task AtualizarCacheProdutosAsync(CancellationToken cancellationToken = default)
    {
        var produtos = (await _produtoRepository.ObterTodosAsync()).ToList();
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);

        foreach (var produto in produtos)
        {
            var existente = await ctx.EstoqueLocal.FindAsync([produto.Id], cancellationToken);
            if (existente is null)
            {
                ctx.EstoqueLocal.Add(new EstoqueLocalCache
                {
                    ProdutoId = produto.Id,
                    Nome = produto.Nome,
                    CodigoInterno = produto.CodigoInterno,
                    Unidade = produto.Unidade,
                    QuantidadeEstoque = produto.QuantidadeEstoque,
                    PrecoVenda = produto.PrecoVenda,
                    AtualizadoEm = DateTime.UtcNow
                });
            }
            else
            {
                existente.Nome = produto.Nome;
                existente.CodigoInterno = produto.CodigoInterno;
                existente.Unidade = produto.Unidade;
                existente.QuantidadeEstoque = produto.QuantidadeEstoque;
                existente.PrecoVenda = produto.PrecoVenda;
                existente.AtualizadoEm = DateTime.UtcNow;
            }
        }

        await ctx.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cache local de estoque atualizado: {Qtd} produtos", produtos.Count);
    }

    public async Task<decimal?> ObterEstoqueLocalAsync(int produtoId, CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        var cache = await ctx.EstoqueLocal.AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProdutoId == produtoId, cancellationToken);
        return cache?.QuantidadeEstoque;
    }

    public async Task<int> ContarPendentesAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = await _factory.CreateDbContextAsync(cancellationToken);
        return await ctx.VendasContingencia.CountAsync(v => v.PendenteSincronizacao, cancellationToken);
    }
}
