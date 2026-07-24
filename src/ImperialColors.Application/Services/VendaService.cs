using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class VendaService : IVendaService, IVendaContingenciaSync
{
    private readonly IVendaRepository _vendaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IMovimentacaoEstoqueRepository _movimentacaoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IDatabaseHealthService _health;
    private readonly IContingencyVendaService _contingency;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<VendaService> _logger;

    public VendaService(
        IVendaRepository vendaRepository,
        IProdutoRepository produtoRepository,
        IMovimentacaoEstoqueRepository movimentacaoRepository,
        IClienteRepository clienteRepository,
        IDatabaseHealthService health,
        IContingencyVendaService contingency,
        IAuditoriaService auditoria,
        ILogger<VendaService> logger)
    {
        _vendaRepository = vendaRepository;
        _produtoRepository = produtoRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _clienteRepository = clienteRepository;
        _health = health;
        _contingency = contingency;
        _auditoria = auditoria;
        _logger = logger;
    }

    public async Task<IEnumerable<VendaDto>> ObterTodosAsync()
    {
        var vendas = await _vendaRepository.ObterTodosAsync();
        return vendas.Select(MapParaDto);
    }

    public async Task<VendaDto?> ObterPorIdAsync(int id)
    {
        var venda = await _vendaRepository.ObterPorIdAsync(id);
        return venda is null ? null : MapParaDto(venda);
    }

    public async Task<VendaDto?> ObterComItensAsync(int id)
    {
        var venda = await _vendaRepository.ObterComItensAsync(id);
        return venda is null ? null : MapParaDto(venda);
    }

    public async Task<IEnumerable<VendaDto>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim)
    {
        var vendas = await _vendaRepository.ObterPorPeriodoAsync(inicio, fim);
        return vendas.Select(MapParaDto);
    }

    public async Task<PaginacaoResultadoDto<VendaDto>> ObterPaginadoPorPeriodoAsync(
        DateTime inicio, DateTime fim, int pagina, int itensPorPagina, string? termoBusca = null,
        CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _vendaRepository.ObterPaginadoPorPeriodoAsync(
            inicio, fim, pagina, itensPorPagina, termoBusca, cancellationToken);

        return new PaginacaoResultadoDto<VendaDto>
        {
            Itens = itens.Select(MapParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    public async Task<VendaDto> CriarAsync(CriarVendaDto dto)
    {
        if (!_health.IsOnline)
            return await SalvarOfflineComAuditoriaAsync(dto);

        try
        {
            return await CriarOnlineInternoAsync(dto, contingenciaId: null);
        }
        catch (Exception ex) when (EhFalhaDeConectividade(ex))
        {
            _health.MarcarOffline();
            _logger.LogWarning(ex, "PostgreSQL indisponível na finalização — ativando contingência offline");
            return await SalvarOfflineComAuditoriaAsync(dto);
        }
    }

    public Task<VendaDto> CriarComContingenciaIdAsync(
        CriarVendaDto dto,
        Guid contingenciaId,
        CancellationToken cancellationToken = default)
        => CriarOnlineInternoAsync(dto, contingenciaId);

    private async Task<VendaDto> SalvarOfflineComAuditoriaAsync(CriarVendaDto dto)
    {
        var venda = await _contingency.SalvarVendaOfflineAsync(dto);
        await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = dto.Usuario ?? "Caixa",
            Modulo = "PDV",
            Acao = "VENDA_CONTINGENCIA_OFFLINE",
            Descricao = $"Venda offline {venda.NumeroVenda} — Total {venda.Total:C}",
            Nivel = NivelLogAuditoria.Warning,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                venda.NumeroVenda,
                venda.Total,
                Itens = venda.Itens.Count
            })
        });
        return venda;
    }

    private async Task<VendaDto> CriarOnlineInternoAsync(CriarVendaDto dto, Guid? contingenciaId)
    {
        if (!dto.Itens.Any())
            throw new DomainException("A venda deve ter pelo menos um item.");

        foreach (var item in dto.Itens)
        {
            var produto = await _produtoRepository.ObterPorIdAsync(item.ProdutoId)
                ?? throw new DomainException($"Produto com Id {item.ProdutoId} não encontrado.");

            if (produto.QuantidadeEstoque < item.Quantidade)
                throw new DomainException($"Estoque insuficiente para '{produto.Nome}'. Disponível: {produto.QuantidadeEstoque} {produto.Unidade}");
        }

        var numeroVenda = await _vendaRepository.GerarNumeroVendaAsync();

        var venda = new Venda
        {
            NumeroVenda = numeroVenda,
            ClienteId = dto.ClienteId,
            Status = StatusVenda.Finalizada,
            Desconto = dto.Desconto,
            Observacoes = dto.Observacoes,
            Usuario = dto.Usuario,
            DataVenda = DateTime.Now,
            ContingenciaId = contingenciaId,
            Itens = dto.Itens.Select(i =>
            {
                var item = new ItemVenda
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    PrecoUnitario = i.PrecoUnitario,
                    Desconto = i.Desconto
                };
                item.CalcularSubtotal();
                return item;
            }).ToList()
        };

        venda.CalcularTotais();
        await ResolverIdentificacaoCompradorAsync(venda, dto);

        var pagamentos = PagamentoHelper.NormalizarPagamentos(dto, venda.Total);
        PagamentoHelper.ValidarPagamentosCompostos(venda.Total, pagamentos);

        var (formaResumo, parcelasResumo, valorPagoResumo, trocoTotal) =
            PagamentoHelper.ResumirPagamentosLegado(pagamentos);

        venda.FormaPagamento = pagamentos.Count > 1 ? formaResumo : pagamentos[0].FormaPagamento;
        venda.QuantidadeParcelas = parcelasResumo;
        venda.ValorPago = valorPagoResumo;
        venda.Troco = trocoTotal;
        venda.Pagamentos = pagamentos.Select((p, index) => new VendaPagamento
        {
            FormaPagamento = p.FormaPagamento,
            Valor = p.Valor,
            ValorRecebido = p.ValorRecebido,
            QuantidadeParcelas = p.QuantidadeParcelas,
            Ordem = index + 1
        }).ToList();

        var vendaCriada = await _vendaRepository.AdicionarAsync(venda);

        foreach (var item in dto.Itens)
        {
            var produto = await _produtoRepository.ObterPorIdAsync(item.ProdutoId)!;
            var quantidadeAnterior = produto!.QuantidadeEstoque;
            produto.QuantidadeEstoque -= item.Quantidade;
            await _produtoRepository.AtualizarAsync(produto);

            await _movimentacaoRepository.AdicionarAsync(new MovimentacaoEstoque
            {
                ProdutoId = item.ProdutoId,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = item.Quantidade,
                QuantidadeAnterior = quantidadeAnterior,
                QuantidadeAtual = produto.QuantidadeEstoque,
                Motivo = contingenciaId.HasValue
                    ? $"Venda #{numeroVenda} (sync contingência)"
                    : $"Venda #{numeroVenda}",
                Usuario = dto.Usuario,
                VendaId = vendaCriada.Id
            });
        }

        _logger.LogInformation("Venda criada: {NumeroVenda} - Total: {Total}", numeroVenda, venda.Total);

        await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = dto.Usuario ?? "Caixa",
            Modulo = "PDV",
            Acao = contingenciaId.HasValue ? "VENDA_SINCRONIZADA" : "VENDA_FINALIZADA",
            Descricao = $"Venda {numeroVenda} finalizada — Total {venda.Total:C}",
            Nivel = NivelLogAuditoria.Info
        });

        var vendaCompleta = await _vendaRepository.ObterComItensAsync(vendaCriada.Id);
        return MapParaDto(vendaCompleta!);
    }

    private static bool EhFalhaDeConectividade(Exception ex)
    {
        for (var atual = ex; atual is not null; atual = atual.InnerException!)
        {
            var nomeTipo = atual.GetType().FullName ?? atual.GetType().Name;
            if (nomeTipo.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ||
                atual is TimeoutException or IOException ||
                atual.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                atual.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                atual.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                atual.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private async Task ResolverIdentificacaoCompradorAsync(Venda venda, CriarVendaDto dto)
    {
        if (dto.ClienteId is > 0)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(dto.ClienteId.Value)
                ?? throw new DomainException("Cliente selecionado não encontrado.");

            venda.ClienteId = cliente.Id;
            venda.NomeCompradorCupom = cliente.Nome;
            venda.TipoPessoaComprador = cliente.TipoPessoa;
            venda.DocumentoCompradorCupom = cliente.TipoPessoa == TipoPessoa.Juridica
                ? cliente.Cnpj
                : cliente.Cpf;
            return;
        }

        if (dto.ConsumidorFinal)
        {
            venda.NomeCompradorCupom = "Consumidor Final";
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.NomeCompradorAvulso))
            throw new DomainException("Informe o nome do comprador ou selecione Consumidor Final.");

        venda.NomeCompradorCupom = dto.NomeCompradorAvulso.Trim();
        venda.DocumentoCompradorCupom = string.IsNullOrWhiteSpace(dto.DocumentoCompradorAvulso)
            ? null
            : dto.DocumentoCompradorAvulso.Trim();
        venda.TipoPessoaComprador = dto.TipoPessoaCompradorAvulso;
    }

    public async Task<VendaDto> FinalizarAsync(int id)
    {
        var venda = await _vendaRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Venda com Id {id} não encontrada.");

        if (venda.Status != StatusVenda.Aberta)
            throw new DomainException("Apenas vendas abertas podem ser finalizadas.");

        venda.Status = StatusVenda.Finalizada;
        await _vendaRepository.AtualizarAsync(venda);

        return MapParaDto(venda);
    }

    public async Task CancelarAsync(int id)
    {
        await _vendaRepository.CancelarComEstornoAsync(id);
        _logger.LogInformation("Venda cancelada com estorno de estoque: {VendaId}", id);
    }

    public async Task ExcluirFisicamenteAsync(int id)
    {
        await _vendaRepository.ExcluirFisicamenteComEstornoAsync(id);
        _logger.LogWarning("Venda excluída permanentemente do banco: {VendaId}", id);
    }

    public async Task<decimal> ObterTotalVendasDiaAsync()
        => await _vendaRepository.ObterTotalVendasDiaAsync(DateTime.Today);

    public async Task<decimal> ObterTotalVendasMesAsync()
        => await _vendaRepository.ObterTotalVendasMesAsync(DateTime.Now.Year, DateTime.Now.Month);

    private static VendaDto MapParaDto(Venda v) => new()
    {
        Id = v.Id,
        NumeroVenda = v.NumeroVenda,
        ClienteId = v.ClienteId,
        ClienteNome = v.Cliente?.Nome,
        NomeCompradorCupom = v.NomeCompradorCupom,
        DocumentoCompradorCupom = v.DocumentoCompradorCupom,
        TipoPessoaComprador = v.TipoPessoaComprador,
        Status = v.Status,
        Subtotal = v.Subtotal,
        Desconto = v.Desconto,
        Total = v.Total,
        FormaPagamento = v.FormaPagamento,
        QuantidadeParcelas = v.QuantidadeParcelas,
        ValorPago = v.ValorPago,
        Troco = v.Troco,
        Pagamentos = v.Pagamentos?
            .OrderBy(p => p.Ordem)
            .Select(p => new VendaPagamentoDto
            {
                Id = p.Id,
                FormaPagamento = p.FormaPagamento,
                Valor = p.Valor,
                ValorRecebido = p.ValorRecebido,
                QuantidadeParcelas = p.QuantidadeParcelas,
                Ordem = p.Ordem
            }).ToList() ?? new(),
        Observacoes = v.Observacoes,
        Usuario = v.Usuario,
        DataVenda = v.DataVenda,
        Itens = v.Itens?.Select(i => new ItemVendaDto
        {
            Id = i.Id,
            ProdutoId = i.ProdutoId,
            NomeProduto = i.Produto?.Nome ?? string.Empty,
            CodigoInterno = i.Produto?.CodigoInterno,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario,
            Desconto = i.Desconto,
            Subtotal = i.Subtotal,
            Unidade = i.Produto?.Unidade ?? "UN"
        }).ToList() ?? new()
    };
}
