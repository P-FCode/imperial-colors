namespace ImperialColors.Domain.Exceptions;

/// <summary>
/// Erro de comunicação/rejeição da API Fiscal externa (PFCode) — carrega o suficiente
/// para o operador entender o que houve e para o suporte correlacionar com o log do
/// servidor (<see cref="TraceId"/>), seguindo o mapa de erros da seção 9 do
/// GUIA_INTEGRACAO.md. Fica em Domain (não em Infrastructure) porque a Application
/// (NotaFiscalService) precisa capturá-la especificamente sem depender de Infrastructure.
/// </summary>
public class FiscalApiException : Exception
{
    /// <summary>Código HTTP da resposta, quando disponível (null em falha de rede).</summary>
    public int? HttpStatus { get; }

    /// <summary>Código de erro da plataforma (ex.: SEFAZ_TIMEOUT, TENANT_CERTIFICATE_MISSING) quando presente.</summary>
    public string? Codigo { get; }
    public string? TraceId { get; }
    public IReadOnlyList<string> Problemas { get; }

    /// <summary>
    /// True para 502/503/504 (SEFAZ indisponível/timeout/circuit breaker) — regra de ouro
    /// da seção 9.5 do guia: NUNCA reemitir direto nesse caso, sempre consultar status antes.
    /// </summary>
    public bool ConsultarStatusAntesDeReemitir => HttpStatus is 502 or 503 or 504;

    public FiscalApiException(
        string message,
        int? httpStatus = null,
        string? codigo = null,
        string? traceId = null,
        IReadOnlyList<string>? problemas = null,
        Exception? inner = null)
        : base(message, inner)
    {
        HttpStatus = httpStatus;
        Codigo = codigo;
        TraceId = traceId;
        Problemas = problemas ?? Array.Empty<string>();
    }
}
