using ImperialColors.Domain.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// O formato "versão@máquina" é a única informação persistida da trava de coordenação entre
/// PDVs. Um valor mal interpretado aqui tem dois jeitos de falhar, ambos ruins: interpretar
/// como válido um lixo qualquer (uma comparação de versão poderia acusar "banco mais novo"
/// por engano e travar um caixa saudável), ou nunca reconhecer um valor legítimo (a trava
/// vira um no-op silencioso). Por isso o round-trip completo é testado, não só o caminho feliz.
/// </summary>
public class RegistroVersaoBancoHelperTests
{
    [Theory]
    [InlineData(1, 3, 0, "DESKTOP-CAIXA2")]
    [InlineData(2, 0, 0, "PDV-01")]
    [InlineData(1, 2, 0, "")]
    public void FormatarDepoisConverter_DevolveOMesmoValor(int maior, int menor, int correcao, string maquina)
    {
        var versao = new Version(maior, menor, correcao);

        var formatado = RegistroVersaoBancoHelper.Formatar(versao, maquina);
        var ok = RegistroVersaoBancoHelper.TentarConverter(formatado, out var versaoLida, out var maquinaLida);

        Assert.True(ok);
        Assert.Equal(versao, versaoLida);
        Assert.Equal(maquina, maquinaLida);
    }

    [Fact]
    public void NomeDeMaquinaComArroba_NaoQuebraOParsing()
    {
        // Split(Separador, 2) garante que só a primeira ocorrência separa — o resto, mesmo
        // contendo '@', vira parte do nome da máquina.
        var formatado = RegistroVersaoBancoHelper.Formatar(new Version(1, 0, 0), "PDV@LOJA-CENTRO");

        Assert.True(RegistroVersaoBancoHelper.TentarConverter(formatado, out var versao, out var maquina));
        Assert.Equal(new Version(1, 0, 0), versao);
        Assert.Equal("PDV@LOJA-CENTRO", maquina);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    // Sem separador: nem parece o formato esperado.
    [InlineData("1.3.0")]
    [InlineData("DESKTOP-CAIXA2")]
    // Separador presente, mas a parte da versão é lixo — não pode virar "0.0.0 válido".
    [InlineData("nao-e-versao@DESKTOP-CAIXA2")]
    public void ValorInvalidoOuAusente_ERecusado(string? valor)
    {
        Assert.False(RegistroVersaoBancoHelper.TentarConverter(valor, out _, out _));
    }
}
