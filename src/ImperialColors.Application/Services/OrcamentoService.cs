using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class OrcamentoService : IOrcamentoService
{
    private const string ModuloAuditoria = "Orçamento";

    private readonly IOrcamentoRepository _orcamentoRepository;
    private readonly IAuditoriaService _auditoria;

    public OrcamentoService(IOrcamentoRepository orcamentoRepository, IAuditoriaService auditoria)
    {
        _orcamentoRepository = orcamentoRepository;
        _auditoria = auditoria;
    }

    public async Task<PaginacaoResultadoDto<OrcamentoDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default)
    {
        var (itens, total) = await _orcamentoRepository.ObterPaginadoAsync(
            pagina, itensPorPagina, termoBusca, cancellationToken);

        return new PaginacaoResultadoDto<OrcamentoDto>
        {
            Itens = itens.Select(MapearParaDto).ToList(),
            PaginaAtual = pagina,
            ItensPorPagina = itensPorPagina,
            TotalItens = total
        };
    }

    public async Task<OrcamentoDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var orcamento = await _orcamentoRepository.ObterComItensAsync(id, cancellationToken);
        return orcamento is null ? null : MapearParaDto(orcamento);
    }

    public async Task<OrcamentoDto> RegistrarAsync(
        RegistrarOrcamentoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Validar(dto);

        var orcamento = new Orcamento
        {
            // NumeroOrcamento é gerado dentro de RegistrarTransacionalAsync, sob advisory
            // lock — ver OrcamentoRepository. Gerar aqui, fora da transação, foi a causa da
            // falha sob concorrência encontrada na auditoria de 15/09.
            ClienteId = dto.ClienteId,
            NomeCliente = dto.NomeCliente.Trim(),
            TelefoneCliente = TextoOuNulo(dto.TelefoneCliente),
            DataOrcamento = Relogio.Agora,
            DataValidade = dto.DataValidade.Date,
            Desconto = dto.Desconto,
            Observacoes = TextoOuNulo(dto.Observacoes),
            Usuario = TextoOuNulo(dto.Usuario),
            Status = StatusOrcamento.Aberto
        };

        var registrado = await _orcamentoRepository.RegistrarTransacionalAsync(
            orcamento, MapearItens(dto.Itens), cancellationToken);

        await RegistrarAuditoriaAsync(
            "ORCAMENTO_CRIADO",
            $"Orçamento {registrado.NumeroOrcamento} criado para {registrado.NomeCliente} — Total {registrado.Total:C}",
            registrado,
            cancellationToken);

        return MapearParaDto(registrado);
    }

    public async Task<OrcamentoDto> AtualizarAsync(
        AtualizarOrcamentoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Id <= 0)
            throw new DomainException("Informe o orçamento a ser editado.");

        Validar(dto);

        var orcamento = new Orcamento
        {
            Id = dto.Id,
            ClienteId = dto.ClienteId,
            NomeCliente = dto.NomeCliente.Trim(),
            TelefoneCliente = TextoOuNulo(dto.TelefoneCliente),
            DataValidade = dto.DataValidade.Date,
            Desconto = dto.Desconto,
            Observacoes = TextoOuNulo(dto.Observacoes),
            Usuario = TextoOuNulo(dto.Usuario)
        };

        var atualizado = await _orcamentoRepository.AtualizarTransacionalAsync(
            orcamento, MapearItens(dto.Itens), cancellationToken);

        await RegistrarAuditoriaAsync(
            "ORCAMENTO_EDITADO",
            $"Orçamento {atualizado.NumeroOrcamento} editado — Total {atualizado.Total:C}",
            atualizado,
            cancellationToken);

        return MapearParaDto(atualizado);
    }

    public async Task<OrcamentoDto> AlterarStatusAsync(
        int id, StatusOrcamento status, CancellationToken cancellationToken = default)
    {
        var atual = await _orcamentoRepository.ObterComItensAsync(id, cancellationToken)
            ?? throw new DomainException($"Orçamento com Id {id} não encontrado.");

        // Máquina de estados: uma vez decidido (aprovado ou recusado), o orçamento não pode
        // ser reaberto nem trocar de decisão — evita dois atendentes decidindo coisas
        // diferentes para o mesmo cliente sem que o segundo saiba que o primeiro já decidiu.
        if (atual.Status != StatusOrcamento.Aberto)
            throw new DomainException(
                $"O orçamento {atual.NumeroOrcamento} já está marcado como {atual.Status} e não pode ser alterado novamente.");

        if (status == StatusOrcamento.Aprovado && atual.DataValidade.Date < Relogio.Agora.Date)
            throw new DomainException(
                $"O orçamento {atual.NumeroOrcamento} venceu em {atual.DataValidade:dd/MM/yyyy} e não pode mais ser aprovado. Gere um novo orçamento com os preços atuais.");

        var atualizado = await _orcamentoRepository.AlterarStatusAsync(id, status, cancellationToken);

        await RegistrarAuditoriaAsync(
            status == StatusOrcamento.Aprovado ? "ORCAMENTO_APROVADO" : "ORCAMENTO_RECUSADO",
            $"Orçamento {atualizado.NumeroOrcamento} marcado como {status}",
            atualizado,
            cancellationToken);

        return MapearParaDto(atualizado);
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var orcamento = await _orcamentoRepository.ObterComItensAsync(id, cancellationToken)
            ?? throw new DomainException($"Orçamento com Id {id} não encontrado.");

        await _orcamentoRepository.RemoverAsync(id);

        await RegistrarAuditoriaAsync(
            "ORCAMENTO_EXCLUIDO",
            $"Orçamento {orcamento.NumeroOrcamento} excluído — Total {orcamento.Total:C}",
            orcamento,
            cancellationToken,
            NivelLogAuditoria.Warning);
    }

    private Task RegistrarAuditoriaAsync(
        string acao,
        string descricao,
        Orcamento orcamento,
        CancellationToken cancellationToken,
        NivelLogAuditoria nivel = NivelLogAuditoria.Info)
        => _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = orcamento.Usuario ?? "Sistema",
            Modulo = ModuloAuditoria,
            Acao = acao,
            Descricao = descricao,
            Nivel = nivel,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                orcamento.NumeroOrcamento,
                orcamento.NomeCliente,
                orcamento.Subtotal,
                orcamento.Desconto,
                orcamento.Total,
                Itens = orcamento.Itens.Count,
                Status = orcamento.Status.ToString()
            })
        }, cancellationToken);

    private static void Validar(RegistrarOrcamentoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NomeCliente))
            throw new DomainException("Informe para quem é o orçamento.");

        if (dto.Itens is null || dto.Itens.Count == 0)
            throw new DomainException("Adicione pelo menos um item ao orçamento.");

        if (dto.DataValidade.Date < Relogio.Agora.Date)
            throw new DomainException("A validade do orçamento não pode ser anterior a hoje.");

        foreach (var item in dto.Itens)
        {
            if (string.IsNullOrWhiteSpace(item.NomeProduto))
                throw new DomainException("Todos os itens devem ter um nome de produto.");

            if (item.Quantidade <= 0)
                throw new DomainException($"Quantidade inválida para '{item.NomeProduto}'.");

            if (item.PrecoUnitario < 0)
                throw new DomainException($"Preço unitário inválido para '{item.NomeProduto}'.");
        }

        if (dto.Desconto < 0)
            throw new DomainException("O desconto não pode ser negativo.");

        var bruto = dto.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        if (dto.Desconto > bruto)
            throw new DomainException("O desconto não pode ser maior que o valor dos itens.");
    }

    private static List<ItemOrcamento> MapearItens(IEnumerable<ItemOrcamentoEntradaDto> itens)
        => itens.Select(i => new ItemOrcamento
        {
            ProdutoId = i.ProdutoId,
            NomeProduto = i.NomeProduto.Trim(),
            CodigoProduto = TextoOuNulo(i.CodigoProduto),
            Unidade = TextoOuNulo(i.Unidade),
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

    private static OrcamentoDto MapearParaDto(Orcamento orcamento)
        => new()
        {
            Id = orcamento.Id,
            NumeroOrcamento = orcamento.NumeroOrcamento,
            ClienteId = orcamento.ClienteId,
            NomeCliente = orcamento.NomeCliente,
            TelefoneCliente = orcamento.TelefoneCliente,
            DataOrcamento = orcamento.DataOrcamento,
            DataValidade = orcamento.DataValidade,
            Subtotal = orcamento.Subtotal,
            Desconto = orcamento.Desconto,
            Total = orcamento.Total,
            Status = orcamento.Status,
            Observacoes = orcamento.Observacoes,
            Usuario = orcamento.Usuario,
            Itens = orcamento.Itens.Select(i => new ItemOrcamentoDto
            {
                Id = i.Id,
                OrcamentoId = i.OrcamentoId,
                ProdutoId = i.ProdutoId,
                NomeProduto = i.NomeProduto,
                CodigoProduto = i.CodigoProduto,
                Unidade = i.Unidade,
                Quantidade = i.Quantidade,
                PrecoUnitario = i.PrecoUnitario,
                Subtotal = i.Subtotal
            }).ToList()
        };

    private static string? TextoOuNulo(string? texto)
        => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
