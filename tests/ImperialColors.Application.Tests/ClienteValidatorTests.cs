using ImperialColors.Application.DTOs;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ClienteValidatorTests
{
    private static ClienteDto DtoPfValido() => new()
    {
        TipoPessoa = TipoPessoa.Fisica,
        Nome = "Cliente Teste"
    };

    [Fact]
    public void Validar_SemNome_LancaDomainException()
    {
        var dto = DtoPfValido();
        dto.Nome = " ";

        var ex = Assert.Throws<DomainException>(() => ClienteValidator.Validar(dto));
        Assert.Contains("Nome", ex.Message);
    }

    [Fact]
    public void Validar_PessoaFisicaSemDocumento_NaoLancaExcecao()
    {
        // CPF continua opcional — nem toda venda exige documento do cliente.
        var ex = Record.Exception(() => ClienteValidator.Validar(DtoPfValido()));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_PessoaFisicaComCpfValido_NaoLancaExcecao()
    {
        var dto = DtoPfValido();
        dto.Cpf = "111.444.777-35";

        var ex = Record.Exception(() => ClienteValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_PessoaFisicaComCpfInvalido_LancaDomainException()
    {
        var dto = DtoPfValido();
        dto.Cpf = "111.444.777-34"; // dígito verificador errado

        var ex = Assert.Throws<DomainException>(() => ClienteValidator.Validar(dto));
        Assert.Contains("CPF", ex.Message);
    }

    [Fact]
    public void Validar_PessoaJuridicaComCnpjValido_NaoLancaExcecao()
    {
        var dto = DtoPfValido();
        dto.TipoPessoa = TipoPessoa.Juridica;
        dto.Cnpj = "11.222.333/0001-81";

        var ex = Record.Exception(() => ClienteValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_PessoaJuridicaComCnpjInvalido_LancaDomainException()
    {
        var dto = DtoPfValido();
        dto.TipoPessoa = TipoPessoa.Juridica;
        dto.Cnpj = "11.222.333/0001-80"; // dígito verificador errado

        var ex = Assert.Throws<DomainException>(() => ClienteValidator.Validar(dto));
        Assert.Contains("CNPJ", ex.Message);
    }
}
