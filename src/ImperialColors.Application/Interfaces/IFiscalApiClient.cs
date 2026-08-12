using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Cliente da API Fiscal externa (PFCode) que emite NF-e/NFC-e — ver
/// <c>C:\Users\Windows\Desktop\Projetos\PFCode\API-NF\GUIA_INTEGRACAO.md</c>. Recebe a
/// <see cref="NotaFiscal"/> já montada (com <c>Itens</c>/<c>Pagamentos</c> carregados) e
/// devolve DTOs normalizados — quem monta o payload JSON exato da API (contratos e regras
/// de serialização) é só a implementação em Infrastructure, a Application nunca vê isso.
/// </summary>
public interface IFiscalApiClient
{
    Task<ResultadoEmissaoFiscalDto> EmitirAsync(
        NotaFiscal nota, EmitenteFiscalDto emitente, string apiKey, string? cscId, string? cscSecret, CancellationToken cancellationToken = default);

    Task<ResultadoEventoFiscalDto> CancelarAsync(
        NotaFiscal nota, string cnpjEmitente, string justificativa, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>Só NF-e — a API rejeita para NFC-e.</summary>
    Task<ResultadoEventoFiscalDto> CartaCorrecaoAsync(
        NotaFiscal nota, string cnpjEmitente, string correcao, int sequencial, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>Só NF-e — não depende de uma nota específica (faixa de numeração).</summary>
    Task<ResultadoInutilizacaoFiscalDto> InutilizarAsync(
        string cnpjEmitente, string cUF, string ano, string serie, string numeroInicial, string numeroFinal,
        string justificativa, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default);

    Task<ResultadoStatusFiscalDto> ConsultarStatusAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>XML <c>nfeProc</c> completo (raw) a partir da cópia local salva pela API
    /// no momento da emissão — pode devolver 404 mesmo com a nota autorizada se foi
    /// emitida por outra instância (seção 7.2 do guia); não é guarda documental.</summary>
    Task<string> ObterXmlAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>PDF do DANFE (A4 para NF-e, bobina 80mm para NFC-e).</summary>
    Task<byte[]> ObterDanfeAsync(
        TipoNotaFiscal tipo, string chaveAcesso, AmbienteEmissaoFiscal ambiente, string apiKey, CancellationToken cancellationToken = default);
}
