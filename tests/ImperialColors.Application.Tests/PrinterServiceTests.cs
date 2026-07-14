using ImperialColors.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class PrinterServiceTests
{
    [Fact]
    public void ListarImpressoras_NaoLancaExcecao()
    {
        var service = new PrinterService(NullLogger<PrinterService>.Instance);
        var impressoras = service.ListarImpressoras();
        Assert.NotNull(impressoras);
    }

    [Fact]
    public void ImpressoraExiste_ComNomeInvalido_RetornaFalse()
    {
        var service = new PrinterService(NullLogger<PrinterService>.Instance);
        Assert.False(service.ImpressoraExiste("Impressora_Inexistente_XYZ_123"));
    }
}
