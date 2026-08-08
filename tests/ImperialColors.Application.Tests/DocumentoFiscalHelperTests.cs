using ImperialColors.Domain.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class DocumentoFiscalHelperTests
{
    // "111.444.777-35" e "11.222.333/0001-81" são os CPF/CNPJ de teste padrão usados
    // em validadores brasileiros — dígitos verificadores conferidos manualmente pelo
    // algoritmo módulo 11 abaixo, não são só "números que parecem certos".

    [Theory]
    [InlineData("11144477735")]
    [InlineData("111.444.777-35")]
    public void CpfValido_ComDigitoVerificadorCorreto_RetornaTrue(string cpf)
        => Assert.True(DocumentoFiscalHelper.CpfValido(cpf));

    [Theory]
    [InlineData("11144477734")] // último dígito trocado
    [InlineData("11144477835")] // dígito do meio trocado
    [InlineData("1114447773")]  // 10 dígitos
    [InlineData("111444777350")] // 12 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void CpfValido_ComDigitoOuTamanhoErrado_RetornaFalse(string? cpf)
        => Assert.False(DocumentoFiscalHelper.CpfValido(cpf));

    [Theory]
    [InlineData("00000000000")]
    [InlineData("11111111111")]
    [InlineData("99999999999")]
    public void CpfValido_ComTodosDigitosIguais_RetornaFalse(string cpf)
        => Assert.False(DocumentoFiscalHelper.CpfValido(cpf));

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("11.222.333/0001-81")]
    public void CnpjValido_ComDigitoVerificadorCorreto_RetornaTrue(string cnpj)
        => Assert.True(DocumentoFiscalHelper.CnpjValido(cnpj));

    [Theory]
    [InlineData("11222333000180")] // último dígito trocado
    [InlineData("11222333000182")]
    [InlineData("1122233300018")]   // 13 dígitos
    [InlineData("112223330001810")] // 15 dígitos
    [InlineData("")]
    [InlineData(null)]
    public void CnpjValido_ComDigitoOuTamanhoErrado_RetornaFalse(string? cnpj)
        => Assert.False(DocumentoFiscalHelper.CnpjValido(cnpj));

    [Theory]
    [InlineData("00000000000000")]
    [InlineData("11111111111111")]
    public void CnpjValido_ComTodosDigitosIguais_RetornaFalse(string cnpj)
        => Assert.False(DocumentoFiscalHelper.CnpjValido(cnpj));
}
