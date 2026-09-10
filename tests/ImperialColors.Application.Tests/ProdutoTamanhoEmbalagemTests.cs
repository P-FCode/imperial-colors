using ImperialColors.Application.DTOs;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Substituiu o antigo <c>LitragemGl</c> (decimal, só para GL, valores fixos 3,6/18) — a
/// importação do catálogo Paraná trouxe embalagens em kg (balde) e em litros por bombona/
/// lata, que um campo assim não conseguia representar. Agora é texto livre, para qualquer
/// unidade, e só afeta a exibição do nome — nada de fiscal ou de cálculo depende dele.
/// </summary>
public class ProdutoTamanhoEmbalagemTests
{
    [Theory]
    [InlineData("GL", "18L")]
    [InlineData("BA", "25 KG")]
    [InlineData("BD", "16L")]
    [InlineData("LA", "18L")]
    [InlineData("LT", "1L")]
    public void NomeExibicao_ApareceParaQualquerUnidade_NaoSoGalao(string unidade, string tamanho)
    {
        var dto = new ProdutoDto { Nome = "Selador", Unidade = unidade, TamanhoEmbalagem = tamanho };

        Assert.Equal($"Selador ({tamanho})", dto.NomeExibicao);
    }

    [Fact]
    public void NomeExibicao_SemTamanhoEmbalagem_DevolveSoONome()
    {
        var dto = new ProdutoDto { Nome = "Pincel", Unidade = "UN", TamanhoEmbalagem = null };

        Assert.Equal("Pincel", dto.NomeExibicao);
    }

    [Fact]
    public void NomeExibicao_TamanhoEmbalagemEmBranco_TrataComoAusente()
    {
        var dto = new ProdutoDto { Nome = "Pincel", Unidade = "UN", TamanhoEmbalagem = "   " };

        Assert.Equal("Pincel", dto.NomeExibicao);
    }
}
