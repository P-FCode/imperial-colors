using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Infrastructure.Fiscal.Contracts;

namespace ImperialColors.Infrastructure.Fiscal;

/// <summary>
/// Implementação de <see cref="IFiscalApiClient"/> contra a API Fiscal externa (PFCode).
/// Usa dois <see cref="HttpClient"/> nomeados (<see cref="ClienteNFe"/>/<see cref="ClienteNFCe"/>)
/// em vez do padrão típico <c>AddHttpClient&lt;TInterface,TImplementation&gt;</c> usado no
/// resto do projeto (ViaCepService etc.) porque esta única interface precisa falar com
/// duas bases de URL diferentes (NF-e :5001 / NFC-e :5002) — ver registro em
/// <c>InfrastructureExtensions.AddInfrastructure</c>.
/// </summary>
public class FiscalApiClient : IFiscalApiClient
{
    public const string ClienteNFe = "FiscalNFe";
    public const string ClienteNFCe = "FiscalNFCe";

    private readonly IHttpClientFactory _httpClientFactory;

    public FiscalApiClient(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<ResultadoEmissaoFiscalDto> EmitirAsync(
        NotaFiscal nota, EmitenteFiscalDto emitente, string apiKey, string? cscId, string? cscSecret,
        CancellationToken cancellationToken = default)
    {
        var payload = NotaFiscalPayloadBuilder.Construir(nota, emitente);
        var rota = nota.Tipo == TipoNotaFiscal.NFCe ? "api/v1/nfce/emitir" : "api/v1/nfe/emitir";
        var http = ObterCliente(nota.Tipo, apiKey, cscId, cscSecret);

        var response = await http.PostAsJsonAsync(rota, payload, FiscalJsonOptions.Padrao, cancellationToken);
        var resultado = await LerRespostaAsync<EmissaoNotaResponse>(response, cancellationToken);

        return new ResultadoEmissaoFiscalDto
        {
            Aprovado = resultado.Aprovado,
            ChaveAcesso = resultado.ChaveAcesso,
            NProt = resultado.Protocolo?.NProt ?? resultado.NProt,
            DhRecbto = ParseDataHora(resultado.DhRecbto),
            CStat = resultado.CStat,
            XMotivo = resultado.XMotivo,
            MensagemTraduzida = resultado.MensagemTraduzida,
            QrCodeUrl = resultado.QrCodeUrl,
            XmlAutorizado = resultado.XmlAutorizado,
            Erro = resultado.Erro,
            Problemas = resultado.Problemas?.Select(p => $"{p.Codigo}: {p.Mensagem}").ToList() ?? new List<string>()
        };
    }

    public async Task<ResultadoEventoFiscalDto> CancelarAsync(
        NotaFiscal nota, string cnpjEmitente, string justificativa, string apiKey, CancellationToken cancellationToken = default)
    {
        var request = new CancelamentoRequest
        {
            ChaveAcesso = nota.ChaveAcesso ?? string.Empty,
            Cnpj = SomenteDigitos(cnpjEmitente),
            NProt = nota.NProt ?? string.Empty,
            Justificativa = justificativa,
            TpAmb = ((int)nota.Ambiente).ToString()
        };

        var rota = nota.Tipo == TipoNotaFiscal.NFCe ? "api/v1/nfce/cancelar" : "api/v1/nfe/cancelar";
        var http = ObterCliente(nota.Tipo, apiKey, null, null);
        var response = await http.PostAsJsonAsync(rota, request, FiscalJsonOptions.Padrao, cancellationToken);
        return MapearEvento(await LerRespostaAsync<EventoNotaResponse>(response, cancellationToken));
    }

    public async Task<ResultadoEventoFiscalDto> CartaCorrecaoAsync(
        NotaFiscal nota, string cnpjEmitente, string correcao, int sequencial, string apiKey, CancellationToken cancellationToken = default)
    {
        var request = new CartaCorrecaoRequest
        {
            ChaveAcesso = nota.ChaveAcesso ?? string.Empty,
            Cnpj = SomenteDigitos(cnpjEmitente),
            Correcao = correcao,
            Sequencial = sequencial,
            TpAmb = ((int)nota.Ambiente).ToString()
        };

        var http = ObterCliente(TipoNotaFiscal.NFe, apiKey, null, null);
        var response = await http.PostAsJsonAsync("api/v1/nfe/carta-correcao", request, FiscalJsonOptions.Padrao, cancellationToken);
        return MapearEvento(await LerRespostaAsync<EventoNotaResponse>(response, cancellationToken));
    }

    public async Task<ResultadoInutilizacaoFiscalDto> InutilizarAsync(
        string cnpjEmitente, string cUF, string ano, string serie, string numeroInicial, string numeroFinal,
        string justificativa, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default)
    {
        var request = new InutilizacaoRequest
        {
            Cnpj = SomenteDigitos(cnpjEmitente),
            CUF = cUF,
            Ano = ano,
            Modelo = "55",
            Serie = serie,
            NumeroInicial = numeroInicial,
            NumeroFinal = numeroFinal,
            Justificativa = justificativa,
            TpAmb = ((int)ambiente).ToString()
        };

        var http = ObterCliente(TipoNotaFiscal.NFe, apiKey, null, null);
        var response = await http.PostAsJsonAsync("api/v1/nfe/inutilizar", request, FiscalJsonOptions.Padrao, cancellationToken);
        var resultado = await LerRespostaAsync<InutilizacaoResponse>(response, cancellationToken);

        return new ResultadoInutilizacaoFiscalDto
        {
            Aprovado = resultado.Aprovado,
            NProt = resultado.NProt,
            CStat = resultado.CStat,
            XMotivo = resultado.XMotivo,
            Faixa = resultado.Faixa,
            Erro = resultado.Erro,
            Problemas = resultado.Problemas?.Select(p => $"{p.Codigo}: {p.Mensagem}").ToList() ?? new List<string>()
        };
    }

    public async Task<ResultadoStatusFiscalDto> ConsultarStatusAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default)
    {
        var http = ObterCliente(tipo, apiKey, null, null);
        var response = await http.GetAsync($"api/v1/notas/status/{chaveAcesso}?tpAmb={(int)ambiente}", cancellationToken);
        var resultado = await LerRespostaAsync<StatusNotaResponse>(response, cancellationToken);

        return new ResultadoStatusFiscalDto
        {
            ChaveAcesso = resultado.ChaveAcesso,
            CStat = resultado.CStat,
            XMotivo = resultado.XMotivo,
            NProt = resultado.NProt,
            DataAutorizacao = ParseDataHora(resultado.DataAutorizacao),
            Situacao = resultado.Situacao,
            ConfirmadoNaSefaz = resultado.ConfirmadoNaSefaz,
            EventosRegistrados = resultado.EventosRegistrados?.Select(e => new EventoRegistradoFiscalDto
            {
                TpEvento = e.TpEvento,
                CStat = e.CStat,
                XMotivo = e.XMotivo,
                NProt = e.NProt,
                NSeqEvento = int.TryParse(e.NSeqEvento, out var seq) ? seq : null,
                DhRegEvento = ParseDataHora(e.DhRegEvento)
            }).ToList() ?? new List<EventoRegistradoFiscalDto>()
        };
    }

    public async Task<string> ObterXmlAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default)
    {
        var http = ObterCliente(tipo, apiKey, null, null);
        var response = await http.GetAsync($"api/v1/notas/xml/{chaveAcesso}?tpAmb={(int)ambiente}", cancellationToken);
        await GarantirSucessoAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<byte[]> ObterDanfeAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default)
    {
        var http = ObterCliente(tipo, apiKey, null, null);
        var response = await http.GetAsync($"api/v1/notas/danfe/{chaveAcesso}?tpAmb={(int)ambiente}", cancellationToken);
        await GarantirSucessoAsync(response, cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private HttpClient ObterCliente(TipoNotaFiscal tipo, string apiKey, string? cscId, string? cscSecret)
    {
        var http = _httpClientFactory.CreateClient(tipo == TipoNotaFiscal.NFCe ? ClienteNFCe : ClienteNFe);
        http.DefaultRequestHeaders.Remove("X-API-Key");
        http.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        if (!string.IsNullOrWhiteSpace(cscId))
            http.DefaultRequestHeaders.Add("X-CSC-Id", cscId);
        if (!string.IsNullOrWhiteSpace(cscSecret))
            http.DefaultRequestHeaders.Add("X-CSC-Secret", cscSecret);

        return http;
    }

    private static ResultadoEventoFiscalDto MapearEvento(EventoNotaResponse resultado) => new()
    {
        Aprovado = resultado.Aprovado,
        ChaveAcesso = resultado.ChaveAcesso,
        NProt = resultado.NProt,
        DhRegEvento = ParseDataHora(resultado.DhRegEvento),
        NSeqEvento = int.TryParse(resultado.NSeqEvento, out var seq) ? seq : null,
        CStat = resultado.CStat,
        XMotivo = resultado.XMotivo,
        MensagemTraduzida = resultado.MensagemTraduzida,
        Erro = resultado.Erro,
        Problemas = resultado.Problemas?.Select(p => $"{p.Codigo}: {p.Mensagem}").ToList() ?? new List<string>()
    };

    private static DateTime? ParseDataHora(string? valor) =>
        DateTimeOffset.TryParse(valor, out var dto) ? dto.LocalDateTime : null;

    private static string SomenteDigitos(string valor) => new(valor.Where(char.IsDigit).ToArray());

    /// <summary>
    /// Lê e desserializa uma resposta 200 (sucesso), ou monta uma <see cref="FiscalApiException"/>
    /// a partir do formato de erro correspondente ao código HTTP (seção 9 do guia) para
    /// qualquer outro status.
    /// </summary>
    private static async Task<T> LerRespostaAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var corpo = await response.Content.ReadFromJsonAsync<T>(FiscalJsonOptions.Padrao, cancellationToken);
            return corpo ?? throw new FiscalApiException("A API Fiscal devolveu uma resposta vazia.", (int)response.StatusCode);
        }

        throw await MontarExcecaoAsync(response, cancellationToken);
    }

    private static async Task GarantirSucessoAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            throw await MontarExcecaoAsync(response, cancellationToken);
    }

    private static async Task<FiscalApiException> MontarExcecaoAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = (int)response.StatusCode;
        var corpo = await response.Content.ReadAsStringAsync(cancellationToken);

        // 502/503/504 (e parte dos 422) vêm em ProblemDetails (RFC 7807) — seção 9.2.
        if (response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
        {
            var problem = TentarDesserializar<FiscalProblemDetails>(corpo);
            return new FiscalApiException(
                problem?.Detail ?? $"A SEFAZ/API Fiscal não respondeu (HTTP {status}). Consulte o status da nota antes de reemitir.",
                status, problem?.Title, problem?.TraceId);
        }

        // 400 — { "erro": "..." }
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var erro400 = TentarDesserializar<Dictionary<string, object>>(corpo);
            var mensagem = erro400 is not null && erro400.TryGetValue("erro", out var v) ? v?.ToString() : null;
            return new FiscalApiException(mensagem ?? "Payload inválido para a API Fiscal.", status);
        }

        // 401/429 — { "error": { "code", "message" } }
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.TooManyRequests)
        {
            var envelope = TentarDesserializar<ErroComEnvelope>(corpo);
            return new FiscalApiException(
                envelope?.Error?.Message ?? "Falha de autenticação/limite com a API Fiscal.",
                status, envelope?.Error?.Code, envelope?.Error?.TraceId);
        }

        // 422 — ProblemDetails OU { erro, problemas[] }
        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var comProblemas = TentarDesserializar<RespostaComProblemas>(corpo);
            if (comProblemas?.Problemas is { Count: > 0 })
            {
                return new FiscalApiException(
                    comProblemas.Erro ?? "Rejeição na validação local antes de transmitir.",
                    status, problemas: comProblemas.Problemas.Select(p => $"{p.Codigo}: {p.Mensagem}").ToList());
            }

            var problem422 = TentarDesserializar<FiscalProblemDetails>(corpo);
            return new FiscalApiException(problem422?.Detail ?? comProblemas?.Erro ?? "Rejeição da API Fiscal (422).",
                status, problem422?.Title, problem422?.TraceId);
        }

        // Qualquer outro status (ex.: 500 — erro interno da própria API Fiscal, fora do
        // nosso controle) — a API usa o mesmo envelope { error: { code, message, traceId } }
        // em vários status além de 401/429, então tenta esse formato antes de desistir e
        // despejar o JSON cru na mensagem.
        var envelopeGenerico = TentarDesserializar<ErroComEnvelope>(corpo);
        if (envelopeGenerico?.Error?.Message is not null)
        {
            return new FiscalApiException(
                envelopeGenerico.Error.Message, status, envelopeGenerico.Error.Code, envelopeGenerico.Error.TraceId);
        }

        return new FiscalApiException($"Erro inesperado da API Fiscal (HTTP {status}): {corpo}", status);
    }

    private static T? TentarDesserializar<T>(string json) where T : class
    {
        // Só engole falha de PARSE (corpo de erro num formato inesperado — o objetivo deste
        // método é "tentar" um formato e cair pro próximo se não bater). Um catch genérico
        // aqui mascarava qualquer outra exceção (ex.: bug futuro num contrato) atrás de uma
        // mensagem de erro genérica em MontarExcecaoAsync, sem log nem rastro do problema real.
        try { return string.IsNullOrWhiteSpace(json) ? null : System.Text.Json.JsonSerializer.Deserialize<T>(json, FiscalJsonOptions.Padrao); }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private sealed class ErroComEnvelope
    {
        public ErroDetalheContract? Error { get; set; }
    }

    private sealed class ErroDetalheContract
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? TraceId { get; set; }
    }

    private sealed class RespostaComProblemas
    {
        public string? Erro { get; set; }
        public List<ProblemaContract>? Problemas { get; set; }
    }
}
