using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class VendaExternaTxtImportHelperTests
{
    [Fact]
    public void ParseArquivo_DeveInterpretarFormatoCodigoBarrasNomeQuantidade()
    {
        const string conteudo = """
            7891234567890;Tinta Branca 18L;2
            ;Cor especial avulsa;1
            """;

        var linhas = VendaExternaTxtImportHelper.ParseArquivo(conteudo);

        Assert.Equal(2, linhas.Count);
        Assert.Equal("7891234567890", linhas[0].CodigoBarras);
        Assert.Equal("Tinta Branca 18L", linhas[0].NomeProduto);
        Assert.Equal(2m, linhas[0].Quantidade);
        Assert.Null(linhas[1].CodigoBarras);
        Assert.Equal("Cor especial avulsa", linhas[1].NomeProduto);
    }

    [Fact]
    public void ParseArquivo_LinhaInvalida_DeveLancarDomainException()
    {
        const string conteudo = "123;Produto";

        var ex = Assert.Throws<DomainException>(() => VendaExternaTxtImportHelper.ParseArquivo(conteudo));
        Assert.Contains("formato inválido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArquivo_ArquivoVazio_DeveLancarDomainException()
    {
        Assert.Throws<DomainException>(() => VendaExternaTxtImportHelper.ParseArquivo("   "));
    }
}
