using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;

namespace ImperialColors.Infrastructure.Services;

/// <summary>
/// Consulta CNPJ com fallback: ReceitaWS (primária) e BrasilAPI (secundária).
/// </summary>
public class CnpjConsultaCompostaService : ICnpjConsultaService
{
    private readonly ReceitaWsCnpjService _receitaWs;
    private readonly BrasilApiCnpjService _brasilApi;

    public CnpjConsultaCompostaService(ReceitaWsCnpjService receitaWs, BrasilApiCnpjService brasilApi)
    {
        _receitaWs = receitaWs;
        _brasilApi = brasilApi;
    }

    public async Task<DadosCnpjDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default)
    {
        try
        {
            var receita = await _receitaWs.ConsultarAsync(cnpj, cancellationToken);
            if (receita is not null)
                return receita;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Tenta BrasilAPI em caso de falha de rede ou limite da ReceitaWS.
        }

        return await _brasilApi.ConsultarAsync(cnpj, cancellationToken);
    }
}
