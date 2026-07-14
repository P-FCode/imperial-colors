using ImperialColors.Application.DTOs;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ProdutoValidatorTests
{
    private static CriarProdutoDto DtoValido() => new()
    {
        CodigoInterno = "P00001",
        Nome = "Tinta Branca",
        CategoriaId = 1,
        MarcaId = 1,
        Custo = 10m,
        PrecoVenda = 20m,
        QuantidadeEstoque = 5,
        EstoqueMinimo = 1,
        Unidade = "UN"
    };

    [Fact]
    public void Validar_DtoCompleto_NaoLancaExcecao()
    {
        var ex = Record.Exception(() => ProdutoValidator.Validar(DtoValido()));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_SemCategoria_LancaDomainException()
    {
        var dto = DtoValido();
        dto.CategoriaId = 0;

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("Categoria", ex.Message);
        Assert.Contains("Marca", ex.Message);
    }

    [Fact]
    public void Validar_SemMarca_LancaDomainException()
    {
        var dto = DtoValido();
        dto.MarcaId = null;

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("Categoria", ex.Message);
    }

    [Fact]
    public void Validar_CustoZeroQuandoInformado_LancaDomainException()
    {
        var dto = DtoValido();
        dto.Custo = 0;

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("custo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validar_CustoNull_NaoLancaExcecao()
    {
        var dto = DtoValido();
        dto.Custo = null;

        var ex = Record.Exception(() => ProdutoValidator.Validar(dto));
        Assert.Null(ex);
    }

    [Fact]
    public void Validar_UnidadeInvalida_LancaDomainException()
    {
        var dto = DtoValido();
        dto.Unidade = "KG";

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("Unidade", ex.Message);
    }

    [Fact]
    public void Validar_PrecoVendaNegativo_LancaDomainException()
    {
        var dto = DtoValido();
        dto.PrecoVenda = -1;

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("venda", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validar_EstoqueNegativo_LancaDomainException()
    {
        var dto = DtoValido();
        dto.QuantidadeEstoque = -1;

        var ex = Assert.Throws<DomainException>(() => ProdutoValidator.Validar(dto));
        Assert.Contains("estoque", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
