using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class VendaExternaService : IVendaExternaService
{
    private readonly IVendaExternaRepository _vendaExternaRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuarioAtual _usuarioAtual;

    public VendaExternaService(
        IVendaExternaRepository vendaExternaRepository,
        IProdutoRepository produtoRepository,
        IAuditoriaService auditoria,
        IUsuarioAtual usuarioAtual)
    {
        _vendaExternaRepository = vendaExternaRepository;
        _produtoRepository = produtoRepository;
        _auditoria = auditoria;
        _usuarioAtual = usuarioAtual;
    }

    public async Task<IEnumerable<VendaExternaDto>> ObterTodosAsync(CancellationToken cancellationToken = default)
    {
        var vendas = await _vendaExternaRepository.ObterTodosComItensAsync(cancellationToken);
        return vendas.Select(MapearParaDto);
    }

    public async Task<IEnumerable<VendaExternaDto>> ObterPorPeriodoAsync(
        DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        var vendas = await _vendaExternaRepository.ObterPorPeriodoAsync(inicio, fim, cancellationToken);
        return vendas.Select(MapearParaDto);
    }

    public async Task<PaginacaoResultadoDto<VendaExternaDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _vendaExternaRepository.ObterPaginadoAsync(pagina, itensPorPagina, termoBusca, cancellationToken);

        return new PaginacaoResultadoDto<VendaExternaDto>
        {
            Itens = itens.Select(MapearParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
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

        ValidarComissao(dto.Comissao, dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario));

        var numero = await _vendaExternaRepository.GerarNumeroVendaExternaAsync(cancellationToken);
        var venda = new VendaExterna
        {
            NumeroVendaExterna = numero,
            Comissao = dto.Comissao,
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
        ValidarComissao(dto.Comissao, dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario));

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
            dto.Id, dto.Observacoes, dto.Comissao, itens, dto.Usuario, cancellationToken);

        return MapearParaDto(atualizada);
    }

    public async Task ExcluirFisicamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        var venda = await _vendaExternaRepository.ObterComItensAsync(id, cancellationToken)
            ?? throw new DomainException($"Venda externa com Id {id} não encontrada.");

        await _vendaExternaRepository.ExcluirFisicamenteTransacionalAsync(id, cancellationToken);

        await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = _usuarioAtual.Nome,
            Modulo = "Vendas Externas",
            Acao = "VENDA_EXTERNA_EXCLUIDA",
            Descricao = $"Venda externa {venda.NumeroVendaExterna} excluída permanentemente — Total {venda.Total:C}",
            Nivel = NivelLogAuditoria.Warning,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                venda.Id, venda.NumeroVendaExterna, venda.DataVenda, venda.Total, venda.Usuario,
                Itens = venda.Itens.Select(i => new { i.ProdutoId, i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal })
            })
        }, cancellationToken);
    }

    public Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ProcessarImportacaoTextoAsync(
        string conteudoArquivo,
        FormatoImportacaoLista formato,
        CancellationToken cancellationToken = default)
        => VincularProdutosAsync(VendaExternaImportHelper.ParseTexto(conteudoArquivo, formato));

    public Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ProcessarImportacaoPlanilhaAsync(
        IReadOnlyList<LinhaBrutaImportacaoDto> celulas,
        CancellationToken cancellationToken = default)
        => VincularProdutosAsync(VendaExternaImportHelper.ParseCelulas(celulas));

    /// <summary>
    /// Completa cada linha do arquivo com o produto do estoque correspondente ao código de
    /// barras — é isso que traz nome e preço de tabela e faz o item entrar na venda como
    /// "Estoque" (com baixa) em vez de "Manual". Linha sem código, ou com código que não
    /// está cadastrado, continua valendo: entra como item manual, com o nome que veio no
    /// arquivo e preço a ser digitado na grade de conferência.
    /// </summary>
    private async Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> VincularProdutosAsync(
        IReadOnlyList<LinhaImportacaoVendaExternaDto> linhas)
    {
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

    /// <summary>
    /// Comissão é opcional (zero = venda sem comissão), mas quando existe não pode passar do
    /// valor vendido: a comissão sai do faturamento, e uma comissão maior que a venda faria a
    /// loja registrar faturamento negativo naquele dia. Negativo também não entra — se foi
    /// digitado com sinal trocado, vira acréscimo no faturamento em vez de desconto.
    /// </summary>
    private static void ValidarComissao(decimal comissao, decimal totalDaVenda)
    {
        if (comissao < 0)
            throw new DomainException("A comissão não pode ser negativa.");

        if (comissao > totalDaVenda)
            throw new DomainException(
                $"A comissão (R$ {comissao:N2}) não pode ser maior que o total da venda (R$ {totalDaVenda:N2}).");
    }

    /// <summary>Quantos pendentes o painel do Dashboard lista — ele responde "quem eu tenho
    /// que pagar?", não substitui a tela de controle, que é paginada.</summary>
    private const int PendentesNoPainel = 10;

    private static bool? TraduzirFiltro(FiltroComissaoVendaExterna filtro) => filtro switch
    {
        FiltroComissaoVendaExterna.APagar => false,
        FiltroComissaoVendaExterna.Pagas => true,
        _ => null
    };

    public async Task<PaginacaoResultadoDto<ComissaoVendaExternaDto>> ObterComissoesPaginadoAsync(
        FiltroComissaoVendaExterna filtro, int pagina, int itensPorPagina, CancellationToken cancellationToken = default)
    {
        var (vendas, total) = await _vendaExternaRepository.ListarComComissaoPaginadoAsync(
            TraduzirFiltro(filtro), pagina, itensPorPagina, cancellationToken);

        return new PaginacaoResultadoDto<ComissaoVendaExternaDto>
        {
            Itens = vendas.Select(MapearComissao).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    public async Task<ResumoComissoesDto> ObterResumoComissoesAsync(CancellationToken cancellationToken = default)
    {
        var hoje = Relogio.Hoje;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);

        var resumo = await _vendaExternaRepository.ObterResumoComissoesAsync(
            inicioMes, inicioMes.AddMonths(1), cancellationToken);

        var pendentes = await _vendaExternaRepository.ListarComComissaoAsync(paga: false, cancellationToken);

        return new ResumoComissoesDto
        {
            TotalAPagar = resumo.TotalAPagar,
            QuantidadeAPagar = resumo.QuantidadeAPagar,
            TotalPago = resumo.TotalPago,
            QuantidadePaga = resumo.QuantidadePaga,
            TotalDoMes = resumo.TotalDoMes,
            Pendentes = pendentes.Take(PendentesNoPainel).Select(MapearComissao).ToList()
        };
    }

    public async Task MarcarComissaoAsync(
        int vendaExternaId, bool paga, CancellationToken cancellationToken = default)
    {
        var venda = await _vendaExternaRepository.MarcarComissaoAsync(vendaExternaId, paga, cancellationToken);

        await _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = _usuarioAtual.Nome,
            Modulo = "Vendas Externas",
            Acao = paga ? "COMISSAO_PAGA" : "COMISSAO_ESTORNADA",
            Descricao = paga
                ? $"Comissão de {venda.Comissao:C} da venda externa {venda.NumeroVendaExterna} marcada como paga"
                : $"Comissão de {venda.Comissao:C} da venda externa {venda.NumeroVendaExterna} voltou para 'a pagar'",
            Nivel = paga ? NivelLogAuditoria.Info : NivelLogAuditoria.Warning
        }, cancellationToken);
    }

    private static ComissaoVendaExternaDto MapearComissao(VendaExterna venda) => new()
    {
        VendaExternaId = venda.Id,
        NumeroVendaExterna = venda.NumeroVendaExterna,
        DataVenda = venda.DataVenda,
        Usuario = venda.Usuario,
        TotalVenda = venda.Total,
        Comissao = venda.Comissao,
        Paga = venda.ComissaoPaga,
        PagaEm = venda.ComissaoPagaEm
    };

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
            Comissao = venda.Comissao,
            ComissaoPaga = venda.ComissaoPaga,
            ComissaoPagaEm = venda.ComissaoPagaEm,
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
