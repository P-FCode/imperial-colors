using ImperialColors.Infrastructure.Security;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Cobre a proteção em repouso dos segredos fiscais (API Key e CSC da NFC-e).
/// A DPAPI é uma API do Windows — estes testes só têm sentido nessa plataforma, que é a
/// única onde o app roda (WPF).
/// </summary>
public class ProtecaoSegredoFiscalTests
{
    private const string CscExemplo = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890";

    [Fact]
    public void Proteger_DepoisDesproteger_DevolveOValorOriginal()
    {
        var protegido = ProtecaoSegredoFiscal.Proteger(CscExemplo);

        Assert.NotEqual(CscExemplo, protegido);
        Assert.Equal(CscExemplo, ProtecaoSegredoFiscal.Desproteger(protegido));
    }

    /// <summary>O ponto da proteção: o segredo não pode aparecer legível num dump do banco.</summary>
    [Fact]
    public void Proteger_NaoDeixaOSegredoLegivelNoValorGravado()
    {
        var protegido = ProtecaoSegredoFiscal.Proteger(CscExemplo);

        Assert.DoesNotContain("A1B2C3D4", protegido);
        Assert.StartsWith("dpapi:v1:", protegido);
    }

    /// <summary>
    /// O texto cifrado é bem maior que o original — foi por isso que as colunas precisaram
    /// virar <c>text</c>. Se algum dia alguém voltar a pôr HasMaxLength nelas, este teste
    /// documenta a ordem de grandeza envolvida.
    /// </summary>
    [Fact]
    public void Proteger_GeraValorMaiorQueOsVarcharAntigos()
    {
        var protegido = ProtecaoSegredoFiscal.Proteger(CscExemplo)!;

        Assert.True(protegido.Length > 64,
            $"O CSC cifrado tem {protegido.Length} caracteres e não caberia no varchar(64) anterior.");
    }

    /// <summary>
    /// Valor gravado antes da proteção existir: precisa continuar legível, senão a atualização
    /// derrubaria a emissão de quem já tinha API Key cadastrada.
    /// </summary>
    [Fact]
    public void Desproteger_ValorLegadoEmTextoPuro_DevolveComoEsta()
    {
        Assert.Equal(CscExemplo, ProtecaoSegredoFiscal.Desproteger(CscExemplo));
    }

    /// <summary>
    /// Banco restaurado em outra máquina: a chave da DPAPI é da máquina, então o valor não
    /// decifra. Precisa devolver null (campo vazio → validação pede o cadastro de novo) em vez
    /// do texto cifrado, que iria para a API Fiscal e voltaria como 401 sem explicação.
    /// </summary>
    [Fact]
    public void Desproteger_CifradoPorOutraMaquina_DevolveNullEmVezDeLixo()
    {
        var deOutraMaquina = "dpapi:v1:" + Convert.ToBase64String("nao-decifravel-aqui"u8.ToArray());

        Assert.Null(ProtecaoSegredoFiscal.Desproteger(deOutraMaquina));
    }

    [Fact]
    public void Proteger_AplicadoDuasVezes_NaoCifraEmDobro()
    {
        var umaVez = ProtecaoSegredoFiscal.Proteger(CscExemplo);
        var duasVezes = ProtecaoSegredoFiscal.Proteger(umaVez);

        Assert.Equal(umaVez, duasVezes);
        Assert.Equal(CscExemplo, ProtecaoSegredoFiscal.Desproteger(duasVezes));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValoresVazios_PassamIntactosNosDoisSentidos(string? valor)
    {
        Assert.Equal(valor, ProtecaoSegredoFiscal.Proteger(valor));
        Assert.Equal(valor, ProtecaoSegredoFiscal.Desproteger(valor));
    }
}
