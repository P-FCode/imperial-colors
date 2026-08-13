using ImperialColors.Domain.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ChaveAcessoNfeHelperTests
{
    [Fact]
    public void GerarCodigoNumerico_RetornaSempreOitoDigitos()
    {
        for (var i = 0; i < 200; i++)
        {
            var codigo = ChaveAcessoNfeHelper.GerarCodigoNumerico();
            Assert.Equal(8, codigo.Length);
            Assert.True(codigo.All(char.IsDigit));
        }
    }

    [Fact]
    public void Montar_ComDadosValidos_CalculaDigitoVerificadorPeloModulo11Oficial()
    {
        // Vetor conferido de forma independente (pesos 2..9 da direita pra esquerda,
        // resto<2 => DV=0, senão DV=11-resto — algoritmo oficial da chave de acesso, seção 7
        // do GUIA_INTEGRACAO.md e confirmado externamente, não o cálculo interno do helper).
        // cUF=41, AAMM=2608, CNPJ=13416624000136, mod=55, serie=001, nNF=000000123, tpEmis=1,
        // cNF=87654321 => DV=4.
        var chave = ChaveAcessoNfeHelper.Montar(
            cUf: "41",
            dataEmissao: new DateTime(2026, 8, 15),
            cnpj: "13.416.624/0001-36",
            modelo: "55",
            serie: "1",
            numero: "123",
            tpEmis: "1",
            codigoNumerico: "87654321");

        Assert.Equal("41260813416624000136550010000001231876543214", chave);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4")]   // menos de 2 dígitos
    public void Montar_ComCUfAusenteOuInvalido_RetornaNull(string? cUf)
    {
        var chave = ChaveAcessoNfeHelper.Montar(
            cUf, DateTime.Now, "13416624000136", "55", "1", "123", "1", "87654321");

        Assert.Null(chave);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1341662400013")] // 13 dígitos
    public void Montar_ComCnpjAusenteOuInvalido_RetornaNull(string? cnpj)
    {
        var chave = ChaveAcessoNfeHelper.Montar(
            "41", DateTime.Now, cnpj, "55", "1", "123", "1", "87654321");

        Assert.Null(chave);
    }
}
