using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.Interfaces;

public interface INotaFiscalService
{
    /// <summary>Monta (em memória, sem persistir) um rascunho a partir de uma venda já
    /// registrada — autopreenche destinatário, itens e pagamentos. O operador revisa e
    /// confirma com <see cref="CriarRascunhoAsync"/>.</summary>
    Task<NotaFiscalDto> MontarRascunhoAPartirDeVendaAsync(int vendaId, TipoNotaFiscal tipo, CancellationToken cancellationToken = default);

    /// <summary>Monta (em memória) um item a partir de um produto do estoque — usado pela
    /// busca de produto na aba "Produtos ou serviços" da tela de emissão.</summary>
    Task<ItemNotaFiscalDto> MontarItemAPartirDeProdutoAsync(
        int produtoId, decimal quantidade, bool interestadual, CancellationToken cancellationToken = default);

    /// <summary>Recalcula os totais (bloco "Cálculo do imposto") a partir dos itens —
    /// chamado pela UI a cada mudança de item quando "Cálculo ligado" está ativo.</summary>
    NotaFiscalDto RecalcularTotais(NotaFiscalDto nota);

    /// <summary>Sincroniza CRT e a tributação de cada item (CST/CSOSN/alíquotas/NCM/CFOP)
    /// com o cadastro ATUAL do produto/categoria/Regra Geral e com o regime tributário
    /// vigente — sem mexer em descrição/código/quantidade/valor (dado comercial, não
    /// fiscal). Use ao abrir um rascunho para edição e antes de emitir, para que uma
    /// correção feita no Estoque ou em Configurações → Fiscal depois que o item foi
    /// adicionado chegue à nota sem precisar clicar "Atualizar" item por item.</summary>
    Task<NotaFiscalDto> SincronizarTributacaoComCadastroAtualAsync(NotaFiscalDto nota, CancellationToken cancellationToken = default);

    Task<NotaFiscalDto> CriarRascunhoAsync(NotaFiscalDto nota, CancellationToken cancellationToken = default);
    Task<NotaFiscalDto> AtualizarRascunhoAsync(NotaFiscalDto nota, CancellationToken cancellationToken = default);

    /// <summary>Só Rascunho ou Rejeitada — nota Autorizada/Cancelada é documento fiscal
    /// (ou já foi transmitido à SEFAZ), não pode ser apagada localmente.</summary>
    Task ExcluirAsync(int notaFiscalId, CancellationToken cancellationToken = default);

    Task<NotaFiscalDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotaFiscalResumoDto>> ListarAsync(TipoNotaFiscal tipo, StatusNotaFiscal? status = null, CancellationToken cancellationToken = default);
    Task<string> ObterProximoNumeroAsync(TipoNotaFiscal tipo, string? serie = null, AmbienteEmissaoFiscal? ambiente = null, CancellationToken cancellationToken = default);

    /// <summary>Painel-resumo da tela de Nota Fiscal (hub) — contadores (NF-e + NFC-e
    /// somadas) e as últimas 10 notas emitidas, ordenadas pela emissão mais recente.</summary>
    Task<ResumoNotasFiscaisDto> ObterResumoAsync(CancellationToken cancellationToken = default);

    Task<NotaFiscalDto> EmitirAsync(int notaFiscalId, CancellationToken cancellationToken = default);
    Task<NotaFiscalDto> CancelarAsync(int notaFiscalId, string justificativa, CancellationToken cancellationToken = default);

    /// <summary>Só NF-e.</summary>
    Task<NotaFiscalDto> CartaCorrecaoAsync(int notaFiscalId, string correcao, CancellationToken cancellationToken = default);

    /// <summary>Só NF-e — não depende de uma nota específica (faixa de numeração).</summary>
    Task<ResultadoInutilizacaoFiscalDto> InutilizarAsync(
        string cUF, string ano, string serie, string numeroInicial, string numeroFinal, string justificativa,
        AmbienteEmissaoFiscal ambiente, CancellationToken cancellationToken = default);

    Task<NotaFiscalDto> ConsultarStatusAsync(int notaFiscalId, CancellationToken cancellationToken = default);

    /// <summary>Prefere o XML já arquivado localmente (gravado na emissão); só consulta a
    /// API se não houver cópia local (seção 7.2 do guia — a API não é guarda documental).</summary>
    Task<string> ObterXmlAsync(int notaFiscalId, CancellationToken cancellationToken = default);
    Task<byte[]> ObterDanfeAsync(int notaFiscalId, CancellationToken cancellationToken = default);
}
