namespace ImperialColors.Infrastructure.Fiscal.Contracts;

/// <summary>
/// Resposta de <c>POST /nfe/emitir</c> e <c>/nfce/emitir</c> — seção 3 do guia.
/// ⚠️ Nunca tratar HTTP 200 como sucesso fiscal: o único campo que decide é
/// <see cref="Aprovado"/>.
/// </summary>
public class EmissaoNotaResponse
{
    public bool Aprovado { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? NProt { get; set; }
    public string? DhRecbto { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? MensagemTraduzida { get; set; }
    public string? CStatLote { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? XmlAssinado { get; set; }
    public string? XmlAutorizado { get; set; }
    public string? CaminhoXmlNotas { get; set; }
    public ProtocoloContract? Protocolo { get; set; }
    public ValidacaoConsultaSefazContract? ValidacaoConsultaSefaz { get; set; }
    public string? Erro { get; set; }
    public List<ProblemaContract>? Problemas { get; set; }
}

public class ProtocoloContract
{
    public string? NProt { get; set; }
    public string? DigVal { get; set; }
    public string? DhRecbto { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? ChNFe { get; set; }
}

public class ValidacaoConsultaSefazContract
{
    public bool ConfirmadoNaSefaz { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? NProt { get; set; }
    public string? EndpointUrl { get; set; }
}

public class ProblemaContract
{
    public string? Codigo { get; set; }
    public string? Mensagem { get; set; }
}

/// <summary>Resposta comum de cancelamento/CC-e/inutilização — seção 8 do guia.</summary>
public class EventoNotaResponse
{
    public bool Aprovado { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? NProt { get; set; }
    public string? DhRegEvento { get; set; }
    public string? NSeqEvento { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? MensagemTraduzida { get; set; }
    public string? CStatLote { get; set; }
    public string? Erro { get; set; }
    public List<ProblemaContract>? Problemas { get; set; }
}

/// <summary>Resposta de <c>POST /nfe/inutilizar</c> — não tem chave de acesso vinculável
/// (o número nunca chegou a ser emitido).</summary>
public class InutilizacaoResponse
{
    public bool Aprovado { get; set; }
    public string? NProt { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? Faixa { get; set; }
    public string? Erro { get; set; }
    public List<ProblemaContract>? Problemas { get; set; }
}

/// <summary>Resposta de <c>GET /notas/status/{chaveAcesso}</c> — seção 7.1 do guia.</summary>
public class StatusNotaResponse
{
    public string? ChaveAcesso { get; set; }
    public string? TpAmb { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? NProt { get; set; }
    public string? DataAutorizacao { get; set; }

    /// <summary>Autorizada | Cancelada | Denegada | Rejeitada | Inexistente | Indeterminada.</summary>
    public string? Situacao { get; set; }
    public bool ConfirmadoNaSefaz { get; set; }
    public List<EventoRegistradoContract>? EventosRegistrados { get; set; }
}

public class EventoRegistradoContract
{
    public string? TpEvento { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? NProt { get; set; }
    public string? NSeqEvento { get; set; }
    public string? DhRegEvento { get; set; }
}

/// <summary>Formato RFC 7807 devolvido pela API em erros de infraestrutura (502/503/504
/// e parte dos 422) — seção 9.2 do guia.</summary>
public class FiscalProblemDetails
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int Status { get; set; }
    public string? Detail { get; set; }
    public string? TraceId { get; set; }
}
