using ImperialColors.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace ImperialColors.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class PrinterService : IPrinterService
{
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(ILogger<PrinterService> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> ListarImpressoras()
    {
        try
        {
            var impressoras = new List<string>();
            foreach (string impressora in PrinterSettings.InstalledPrinters)
                impressoras.Add(impressora);

            return impressoras;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível listar impressoras instaladas.");
            return Array.Empty<string>();
        }
    }

    public string? ObterImpressoraPadraoSistema()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.IsValid ? settings.PrinterName : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível obter a impressora padrão do sistema.");
            return null;
        }
    }

    public bool ImpressoraExiste(string nomeImpressora)
    {
        if (string.IsNullOrWhiteSpace(nomeImpressora))
            return false;

        return ListarImpressoras().Any(i =>
            string.Equals(i, nomeImpressora, StringComparison.OrdinalIgnoreCase));
    }
}
