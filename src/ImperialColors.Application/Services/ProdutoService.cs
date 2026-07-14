using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class ProdutoService : IProdutoService
{
    public const string MensagemExclusaoBloqueadaPorHistorico =
        "Este produto não pode ser excluído permanentemente porque já possui movimentações ou vendas registradas no sistema.";

    public const string MensagemCodigoBarrasDuplicado =
        "Este código de barras já está cadastrado para outro produto.";

    private readonly IProdutoRepository _produtoRepository;
    private readonly IMovimentacaoEstoqueRepository _movimentacaoRepository;
    private readonly IRepository<Categoria> _categoriaRepository;
    private readonly IRepository<Marca> _marcaRepository;
    private readonly ILogger<ProdutoService> _logger;

    public ProdutoService(
        IProdutoRepository produtoRepository,
        IMovimentacaoEstoqueRepository movimentacaoRepository,
        IRepository<Categoria> categoriaRepository,
        IRepository<Marca> marcaRepository,
        ILogger<ProdutoService> logger)
    {
        _produtoRepository = produtoRepository;
        _movimentacaoRepository = movimentacaoRepository;
        _categoriaRepository = categoriaRepository;
        _marcaRepository = marcaRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<ProdutoDto>> ObterTodosAsync()
    {
        var produtos = await _produtoRepository.ObterComCategoriaEMarcaAsync();
        return produtos.Select(MapParaDto);
    }

    public async Task<PaginacaoResultadoDto<ProdutoDto>> ObterPaginadoAsync(
        int pagina,
        int itensPorPagina,
        string? termoBusca = null,
        bool apenasPromocao = false,
        CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _produtoRepository.ObterPaginadoAsync(
            pagina, itensPorPagina, termoBusca, apenasPromocao, cancellationToken);

        return new PaginacaoResultadoDto<ProdutoDto>
        {
            Itens = itens.Select(MapParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    public async Task<ProdutoDto?> ObterPorIdAsync(int id)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(id);
        return produto is null ? null : MapParaDto(produto);
    }

    public async Task<ProdutoDto?> ObterPorCodigoBarrasAsync(string codigoBarras)
    {
        var produto = await _produtoRepository.ObterPorCodigoBarrasAsync(codigoBarras);
        return produto is null ? null : MapParaDto(produto);
    }

    public async Task<ProdutoDto?> ObterPorCodigoInternoAsync(string codigoInterno)
    {
        var produto = await _produtoRepository.ObterPorCodigoInternoAsync(codigoInterno);
        return produto is null ? null : MapParaDto(produto);
    }

    public async Task<IEnumerable<ProdutoDto>> BuscarAsync(string termo)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return await ObterTodosAsync();

        var porNome = await _produtoRepository.BuscarPorNomeAsync(termo);
        var porCodigo = await _produtoRepository.ObterPorCodigoInternoAsync(termo);
        var porBarras = await _produtoRepository.ObterPorCodigoBarrasAsync(termo);

        var resultado = porNome.ToList();
        if (porCodigo is not null && !resultado.Any(p => p.Id == porCodigo.Id))
            resultado.Add(porCodigo);
        if (porBarras is not null && !resultado.Any(p => p.Id == porBarras.Id))
            resultado.Add(porBarras);

        return resultado.Select(MapParaDto);
    }

    public async Task<ProdutoDto> CriarAsync(CriarProdutoDto dto)
    {
        ProdutoValidator.Validar(dto);
        await ValidarReferenciasCatalogoAsync(dto.CategoriaId!.Value, dto.MarcaId!.Value);
        await ValidarCodigoBarrasUnicoAsync(dto.CodigoBarras);

        var codigoInterno = InputSanitizer.SanitizarTexto(dto.CodigoInterno, 50);
        var codigoManual = dto.CodigoInternoDefinidoManualmente;

        if (await _produtoRepository.CodigoInternoExisteAsync(codigoInterno))
        {
            if (codigoManual)
                throw new DomainException("Este código interno já está em uso por outro produto.");

            codigoInterno = await GerarProximoCodigoInternoDisponivelAsync();
        }

        var unidade = UnidadesMedida.Normalizar(dto.Unidade);
        var produto = new Produto
        {
            CodigoInterno = codigoInterno,
            CodigoBarras = InputSanitizer.SanitizarTexto(dto.CodigoBarras, 50),
            Nome = InputSanitizer.SanitizarTexto(dto.Nome, 200),
            CategoriaId = dto.CategoriaId,
            MarcaId = dto.MarcaId,
            QuantidadeEstoque = dto.QuantidadeEstoque,
            EstoqueMinimo = dto.EstoqueMinimo,
            Unidade = unidade,
            LitragemGl = unidade == "GL" ? dto.LitragemGl : null,
            Custo = dto.Custo,
            PrecoVenda = dto.PrecoVenda,
            PromocaoAtiva = dto.PromocaoAtiva,
            PrecoPromocional = dto.PromocaoAtiva ? dto.PrecoPromocional : null,
            DataValidade = dto.DataValidade?.Date,
            FornecedorId = dto.FornecedorId,
            Observacoes = InputSanitizer.SanitizarTexto(dto.Observacoes, 500)
        };

        var criado = await _produtoRepository.InserirProdutoAsync(
            produto,
            permitirRegenerarCodigoInterno: !codigoManual,
            obterProximoCodigoInternoAsync: () => RegenerarCodigoInternoAsync(produto.Nome, produto.CodigoInterno));

        if (dto.QuantidadeEstoque > 0)
        {
            await _movimentacaoRepository.AdicionarAsync(new MovimentacaoEstoque
            {
                ProdutoId = criado.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = dto.QuantidadeEstoque,
                QuantidadeAnterior = 0,
                QuantidadeAtual = dto.QuantidadeEstoque,
                Motivo = "Estoque inicial"
            });
        }

        _logger.LogInformation("Produto criado: {Nome} ({CodigoInterno})", dto.Nome, criado.CodigoInterno);
        return MapParaDto(criado);
    }

    public async Task<ProdutoDto> AtualizarAsync(int id, AtualizarProdutoDto dto)
    {
        ProdutoValidator.Validar(dto);
        await ValidarReferenciasCatalogoAsync(dto.CategoriaId!.Value, dto.MarcaId!.Value);
        await ValidarCodigoBarrasUnicoAsync(dto.CodigoBarras, id);

        var produto = await _produtoRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Produto com Id {id} não encontrado.");

        if (await _produtoRepository.CodigoInternoExisteAsync(dto.CodigoInterno, id))
            throw new DomainException("Este código interno já está em uso por outro produto.");

        produto.CodigoInterno = InputSanitizer.SanitizarTexto(dto.CodigoInterno, 50);
        produto.CodigoBarras = InputSanitizer.SanitizarTexto(dto.CodigoBarras, 50);
        produto.Nome = InputSanitizer.SanitizarTexto(dto.Nome, 200);
        produto.CategoriaId = dto.CategoriaId;
        produto.MarcaId = dto.MarcaId;
        produto.EstoqueMinimo = dto.EstoqueMinimo;
        var unidadeAtualizada = UnidadesMedida.Normalizar(dto.Unidade);
        produto.Unidade = unidadeAtualizada;
        produto.LitragemGl = unidadeAtualizada == "GL" ? dto.LitragemGl : null;
        produto.Custo = dto.Custo;
        produto.PrecoVenda = dto.PrecoVenda;
        produto.PromocaoAtiva = dto.PromocaoAtiva;
        produto.PrecoPromocional = dto.PromocaoAtiva ? dto.PrecoPromocional : null;
        produto.DataValidade = dto.DataValidade?.Date;
        produto.FornecedorId = dto.FornecedorId;
        produto.Observacoes = InputSanitizer.SanitizarTexto(dto.Observacoes, 500);

        var quantidadeAnterior = produto.QuantidadeEstoque;
        produto.QuantidadeEstoque = dto.QuantidadeEstoque;

        var atualizado = await _produtoRepository.AtualizarAsync(produto);

        if (quantidadeAnterior != dto.QuantidadeEstoque)
        {
            await _movimentacaoRepository.AdicionarAsync(new MovimentacaoEstoque
            {
                ProdutoId = id,
                Tipo = TipoMovimentacao.Ajuste,
                Quantidade = Math.Abs(dto.QuantidadeEstoque - quantidadeAnterior),
                QuantidadeAnterior = quantidadeAnterior,
                QuantidadeAtual = dto.QuantidadeEstoque,
                Motivo = "Ajuste manual via edição de produto",
                Usuario = "Administrador"
            });
        }

        _logger.LogInformation("Produto atualizado: {Nome} ({Id})", dto.Nome, id);
        return MapParaDto(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        _ = await _produtoRepository.ObterPorIdAsync(id)
            ?? throw new DomainException($"Produto com Id {id} não encontrado.");

        if (await _produtoRepository.PossuiHistoricoComercialAsync(id))
            throw new DomainException(MensagemExclusaoBloqueadaPorHistorico);

        await _produtoRepository.RemoverFisicamenteAsync(id);

        if (await _produtoRepository.ExisteFisicamenteAsync(id))
            throw new DomainException("Não foi possível excluir o produto. Tente novamente.");

        _logger.LogInformation("Produto excluído (hard delete): Id={Id}", id);
    }

    public async Task<bool> CodigoBarrasExisteAsync(
        string codigoBarras,
        int? ignorarProdutoId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizado = InputSanitizer.SanitizarTexto(codigoBarras, 50);
        if (string.IsNullOrWhiteSpace(normalizado))
            return false;

        return await _produtoRepository.CodigoBarrasExisteAsync(normalizado, ignorarProdutoId, cancellationToken);
    }

    public async Task<IEnumerable<ProdutoDto>> ObterComEstoqueBaixoAsync()
    {
        var produtos = await _produtoRepository.ObterComEstoqueBaixoAsync();
        return produtos.Select(MapParaDto);
    }

    public async Task<IEnumerable<ProdutoDto>> ObterSemEstoqueAsync()
    {
        var produtos = await _produtoRepository.ObterSemEstoqueAsync();
        return produtos.Select(MapParaDto);
    }

    public async Task RegistrarMovimentacaoAsync(MovimentacaoEstoqueDto dto)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(dto.ProdutoId)
            ?? throw new DomainException($"Produto com Id {dto.ProdutoId} não encontrado.");

        var quantidadeAnterior = produto.QuantidadeEstoque;
        decimal quantidadeAtual;

        switch (dto.Tipo)
        {
            case TipoMovimentacao.Entrada:
                quantidadeAtual = quantidadeAnterior + dto.Quantidade;
                break;
            case TipoMovimentacao.Saida:
                if (quantidadeAnterior < dto.Quantidade)
                    throw new DomainException($"Estoque insuficiente. Disponível: {quantidadeAnterior} {produto.Unidade}");
                quantidadeAtual = quantidadeAnterior - dto.Quantidade;
                break;
            case TipoMovimentacao.Ajuste:
                quantidadeAtual = dto.Quantidade;
                break;
            default:
                throw new DomainException("Tipo de movimentação inválido.");
        }

        produto.QuantidadeEstoque = quantidadeAtual;
        await _produtoRepository.AtualizarAsync(produto);

        await _movimentacaoRepository.AdicionarAsync(new MovimentacaoEstoque
        {
            ProdutoId = dto.ProdutoId,
            Tipo = dto.Tipo,
            Quantidade = dto.Tipo == TipoMovimentacao.Ajuste ? Math.Abs(quantidadeAtual - quantidadeAnterior) : dto.Quantidade,
            QuantidadeAnterior = quantidadeAnterior,
            QuantidadeAtual = quantidadeAtual,
            Motivo = dto.Motivo,
            Usuario = dto.Usuario
        });
    }

    public async Task<string> GerarProximoCodigoInternoAsync()
        => await GerarProximoCodigoInternoDisponivelAsync();

    public async Task<string> GerarCodigoInternoPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var sigla = ProdutoCodigoIniciaisHelper.ExtrairSigla(nome);
        if (string.IsNullOrWhiteSpace(sigla))
            return await GerarProximoCodigoInternoDisponivelAsync();

        var maiorSequencia = await _produtoRepository.ObterMaiorSequenciaPorSiglaAsync(sigla, cancellationToken);
        return ProdutoCodigoIniciaisHelper.FormatarCodigo(sigla, maiorSequencia + 1);
    }

    private async Task<string> RegenerarCodigoInternoAsync(string nome, string? codigoAtual)
    {
        if (!string.IsNullOrWhiteSpace(codigoAtual)
            && ProdutoCodigoIniciaisHelper.EhCodigoPorIniciais(codigoAtual))
        {
            var sigla = codigoAtual[..^3].ToUpperInvariant();
            var maiorSequencia = await _produtoRepository.ObterMaiorSequenciaPorSiglaAsync(sigla);
            return ProdutoCodigoIniciaisHelper.FormatarCodigo(sigla, maiorSequencia + 1);
        }

        return await GerarCodigoInternoPorNomeAsync(nome);
    }

    private async Task<string> GerarProximoCodigoInternoDisponivelAsync()
    {
        var maiorSequencia = await _produtoRepository.ObterMaiorSequenciaCodigoInternoAsync();
        return ProdutoCodigoInternoHelper.FormatarSequencia(maiorSequencia + 1);
    }

    private async Task ValidarCodigoBarrasUnicoAsync(string? codigoBarras, int? ignorarProdutoId = null)
    {
        if (await CodigoBarrasExisteAsync(codigoBarras ?? string.Empty, ignorarProdutoId))
            throw new DomainException(MensagemCodigoBarrasDuplicado);
    }

    private async Task ValidarReferenciasCatalogoAsync(int categoriaId, int marcaId)
    {
        if (categoriaId <= 0 || marcaId <= 0)
            throw new DomainException("Selecione uma Categoria e uma Marca válidas.");

        if (!await _categoriaRepository.ExisteAsync(categoriaId))
            throw new DomainException(
                $"A categoria selecionada (Id={categoriaId}) não existe no banco de dados. " +
                "Selecione ou cadastre uma categoria válida.");

        if (!await _marcaRepository.ExisteAsync(marcaId))
            throw new DomainException(
                $"A marca selecionada (Id={marcaId}) não existe no banco de dados. " +
                "Selecione ou cadastre uma marca válida.");
    }

    private static ProdutoDto MapParaDto(Produto p) => new()
    {
        Id = p.Id,
        CodigoInterno = p.CodigoInterno,
        CodigoBarras = p.CodigoBarras,
        Nome = p.Nome,
        CategoriaId = p.CategoriaId,
        CategoriaNome = p.Categoria?.Nome,
        MarcaId = p.MarcaId,
        MarcaNome = p.Marca?.Nome,
        QuantidadeEstoque = p.QuantidadeEstoque,
        EstoqueMinimo = p.EstoqueMinimo,
        Unidade = p.Unidade,
        LitragemGl = p.LitragemGl,
        Custo = p.Custo,
        PrecoVenda = p.PrecoVenda,
        PromocaoAtiva = p.PromocaoAtiva,
        PrecoPromocional = p.PrecoPromocional,
        DataValidade = p.DataValidade,
        FornecedorId = p.FornecedorId,
        FornecedorNome = p.Fornecedor?.Nome,
        Observacoes = p.Observacoes
    };
}
