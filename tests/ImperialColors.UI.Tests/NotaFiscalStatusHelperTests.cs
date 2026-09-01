using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// O painel-resumo do hub de Nota Fiscal virou uma pílula colorida por status
/// (<c>NotaFiscalHubView</c>). Todo valor de <see cref="StatusNotaFiscal"/> precisa ter uma
/// cor definida — um valor esquecido não lança exceção (os switches têm <c>_ =></c>), mas
/// cairia silenciosamente no tom "em andamento" (amarelo), o que seria enganoso para um
/// status como Denegada. Este teste força quem adicionar um valor novo ao enum a decidir a
/// cor dele, em vez de herdar o fallback sem perceber.
/// </summary>
public class NotaFiscalStatusHelperTests
{
    public static IEnumerable<object[]> TodosOsStatus() =>
        Enum.GetValues<StatusNotaFiscal>().Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(TodosOsStatus))]
    public void TodoStatus_TemDescricaoECoresDefinidas(StatusNotaFiscal status)
    {
        Assert.False(string.IsNullOrWhiteSpace(NotaFiscalStatusHelper.Descricao(status)));
        Assert.NotNull(NotaFiscalStatusHelper.CorFundo(status));
        Assert.NotNull(NotaFiscalStatusHelper.CorTexto(status));
    }

    /// <summary>Autorizada e Cancelada precisam ser visualmente opostas — são os dois
    /// desfechos que o operador mais precisa distinguir num relance.</summary>
    [Fact]
    public void AutorizadaECancelada_UsamCoresDiferentes()
    {
        Assert.NotEqual(
            NotaFiscalStatusHelper.CorFundo(StatusNotaFiscal.Autorizada).ToString(),
            NotaFiscalStatusHelper.CorFundo(StatusNotaFiscal.Cancelada).ToString());
    }

    /// <summary>Rejeitada é uma situação que ainda dá para corrigir e reemitir — não pode
    /// levar a mesma cor "descartada" de Cancelada/Denegada, ou o operador não vê diferença
    /// entre "ainda dá para agir" e "está definitivamente encerrada".</summary>
    [Fact]
    public void Rejeitada_NaoUsaAMesmaCorDeCanceladaOuDenegada()
    {
        var corRejeitada = NotaFiscalStatusHelper.CorFundo(StatusNotaFiscal.Rejeitada).ToString();

        Assert.NotEqual(corRejeitada, NotaFiscalStatusHelper.CorFundo(StatusNotaFiscal.Cancelada).ToString());
        Assert.NotEqual(corRejeitada, NotaFiscalStatusHelper.CorFundo(StatusNotaFiscal.Denegada).ToString());
    }
}
