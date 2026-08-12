using System.Linq;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Infrastructure.Helpers;

namespace ImperialColors.UI.Helpers;

public static class ExceptionMessageHelper
{
    public static string ObterMensagemAmigavel(Exception exception)
    {
        if (exception is DomainException domain)
            return domain.Message;

        // FiscalApiException não é erro de banco — é a API Fiscal externa (PFCode)
        // respondendo com falha (rede, HTTP 4xx/5xx, rejeição da SEFAZ). Tratar antes do
        // fallback genérico evita rotular como "Erro real do banco" algo que não tem
        // nenhuma relação com o Postgres.
        if (exception is FiscalApiException fiscal)
        {
            var mensagem = fiscal.Message;

            // Problemas[] traz o detalhamento real (código + mensagem por regra violada) — sem
            // isso o operador só via a frase genérica "Rejeição Local: Inconsistências
            // detectadas na pipeline de validação.", sem saber qual campo corrigir.
            if (fiscal.Problemas.Count > 0)
                mensagem += "\n\n" + string.Join("\n", fiscal.Problemas.Select(p => $"• {p}"));

            if (!string.IsNullOrWhiteSpace(fiscal.TraceId))
                mensagem += $"\n\nTraceId: {fiscal.TraceId}";
            return mensagem;
        }

        // Erro de desserialização da resposta da API Fiscal (contrato JSON não bateu com o
        // esperado) também não é erro de banco — sem este ramo, cai no fallback genérico
        // abaixo e vira um enganoso "Erro real do banco: Cannot get the value of a token
        // type 'Number' as a string." (ver StringOrNumberConverter para a blindagem contra
        // a causa mais comum disso).
        if (exception is System.Text.Json.JsonException json)
            return $"A API Fiscal devolveu uma resposta em formato inesperado: {json.Message}";

        var detalhe = DatabaseExceptionHelper.ObterMensagemDetalhada(exception);
        if (!string.IsNullOrWhiteSpace(detalhe) &&
            !detalhe.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase))
        {
            return detalhe.StartsWith("Erro real do banco:", StringComparison.OrdinalIgnoreCase)
                ? detalhe
                : $"Erro real do banco: {detalhe}";
        }

        return exception.Message;
    }
}
