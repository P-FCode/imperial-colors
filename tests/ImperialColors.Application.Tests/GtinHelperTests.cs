using ImperialColors.Domain.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Trava o algoritmo de dígito verificador GS1 usado para decidir se o "Código de Barras"
/// de um produto pode ser enviado como GTIN real (<c>cEAN</c>/<c>cEANTrib</c>) na emissão,
/// ou se deve cair para o literal "SEM GTIN" — ver uso em
/// <c>Infrastructure.Fiscal.NotaFiscalPayloadBuilder.ConstruirItem</c>. Sem essa blindagem,
/// um código interno/PLU guardado no campo de código de barras do produto era mandado cru
/// como GTIN e a SEFAZ rejeitava a nota com "GTIN inválido".
/// </summary>
public class GtinHelperTests
{
    [Theory]
    [InlineData("4006381333931")] // GTIN-13 válido
    [InlineData("40170725")]      // GTIN-8 válido
    public void EhValido_AceitaGtinComDigitoVerificadorCorreto(string gtin)
        => Assert.True(GtinHelper.EhValido(gtin));

    [Theory]
    [InlineData("4006381333930")] // dígito verificador errado (13 dígitos)
    [InlineData("40170720")]      // dígito verificador errado (8 dígitos)
    [InlineData("PDV6501")]       // código interno, não numérico
    [InlineData("12345")]         // tamanho inválido (nem 8/12/13/14)
    [InlineData("SEM GTIN")]      // literal usado quando não há GTIN
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EhValido_RejeitaQualquerCoisaQueNaoSejaUmGtinReal(string? valor)
        => Assert.False(GtinHelper.EhValido(valor));
}
