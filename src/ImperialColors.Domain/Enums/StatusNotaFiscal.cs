namespace ImperialColors.Domain.Enums;

/// <summary>
/// Situação local da nota fiscal — combina o ciclo de vida do rascunho (antes de
/// transmitir) com a <c>situacao</c> normalizada que a API Fiscal devolve em
/// <c>GET /notas/status/{chave}</c> (seção 7.1 do GUIA_INTEGRACAO.md).
/// </summary>
public enum StatusNotaFiscal
{
    /// <summary>Ainda não foi enviada à SEFAZ — só existe no banco local.</summary>
    Rascunho = 0,

    /// <summary>Requisição de emissão em andamento (aguardando resposta da API).</summary>
    Emitindo = 1,

    /// <summary>SEFAZ autorizou o uso da nota (cStat 100/150 confirmado).</summary>
    Autorizada = 2,

    /// <summary>SEFAZ rejeitou a emissão (aprovado=false) — numeração consumida, não reaproveitável.</summary>
    Rejeitada = 3,

    /// <summary>Cancelamento homologado pela SEFAZ.</summary>
    Cancelada = 4,

    /// <summary>SEFAZ negou por irregularidade cadastral do emitente/destinatário.</summary>
    Denegada = 5,

    /// <summary>Faixa de numeração inutilizada (não chegou a ser emitida).</summary>
    Inutilizada = 6,

    /// <summary>Erro de comunicação/timeout antes de uma resposta conclusiva — consultar
    /// status antes de reemitir (seção 9.5 do guia), nunca tratar como rejeição.</summary>
    Indeterminada = 7
}
