using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class BinarySearchCollectionHelperTests
{
    private sealed record ItemTeste(int Id, string Nome);

    [Fact]
    public void FindById_ListaOrdenada_RetornaItemCorreto()
    {
        var itens = BinarySearchCollectionHelper.OrdenarPorId(
            new[] { new ItemTeste(10, "B"), new ItemTeste(3, "A"), new ItemTeste(7, "C") },
            i => i.Id);

        var encontrado = BinarySearchCollectionHelper.FindById(itens, 7, i => i.Id);

        Assert.NotNull(encontrado);
        Assert.Equal("C", encontrado!.Nome);
    }

    [Fact]
    public void FindById_IdInexistente_RetornaNull()
    {
        var itens = BinarySearchCollectionHelper.OrdenarPorId(
            new[] { new ItemTeste(1, "A"), new ItemTeste(2, "B") },
            i => i.Id);

        Assert.Null(BinarySearchCollectionHelper.FindById(itens, 99, i => i.Id));
    }
}
