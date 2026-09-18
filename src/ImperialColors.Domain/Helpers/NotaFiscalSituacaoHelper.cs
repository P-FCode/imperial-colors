using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Classifica o <see cref="StatusNotaFiscal"/> de uma nota já vinculada a uma venda para
/// responder duas perguntas do fluxo "faturar a venda": essa venda ainda pode gerar nota,
/// e existe um documento pela metade para retomar?
///
/// A regra mora aqui (Domínio) porque vale tanto para o Service — que recusa montar um
/// segundo rascunho — quanto para a tela de Vendas, que decide entre bloquear, oferecer o
/// rascunho existente ou seguir para a emissão. Duas cópias do mesmo switch acabariam
/// divergindo, e a divergência aqui significa faturar a mesma receita duas vezes.
/// </summary>
public static class NotaFiscalSituacaoHelper
{
    /// <summary>
    /// A venda já tem documento fiscal vivo — emitir outro seria dobrar imposto sobre uma
    /// receita que só existiu uma vez.
    ///
    /// <see cref="StatusNotaFiscal.Emitindo"/> e <see cref="StatusNotaFiscal.Indeterminada"/>
    /// entram junto com a Autorizada de propósito: nos dois a SEFAZ pode já ter autorizado
    /// sem que a resposta tenha chegado de volta (seção 9.5 do guia manda consultar status
    /// antes de qualquer reenvio). Tratar "não sei" como "não emitida" é exatamente o
    /// caminho para a nota em duplicidade.
    ///
    /// Cancelada, Denegada e Inutilizada NÃO bloqueiam: nesses casos não existe documento
    /// válido cobrindo a venda, e refaturar é o procedimento correto.
    /// </summary>
    public static bool BloqueiaNovaNota(StatusNotaFiscal status) => status
        is StatusNotaFiscal.Autorizada
        or StatusNotaFiscal.Emitindo
        or StatusNotaFiscal.Indeterminada;

    /// <summary>
    /// Nota da venda que ainda não virou documento fiscal e continua editável — rascunho
    /// salvo e não emitido, ou rejeitada pela SEFAZ (corrigir e reemitir é o fluxo normal;
    /// a renumeração acontece sozinha na emissão). Em vez de montar um rascunho novo, a
    /// tela de Vendas oferece retomar este.
    /// </summary>
    public static bool PodeRetomar(StatusNotaFiscal status) => status
        is StatusNotaFiscal.Rascunho
        or StatusNotaFiscal.Rejeitada;
}
