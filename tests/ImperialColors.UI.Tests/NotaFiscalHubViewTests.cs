using System.Windows.Controls;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Construir a tela de verdade valida o XAML inteiro de uma vez — recurso de tema que não
/// existe, nome de brush errado, <c>Binding</c> para uma propriedade que não existe no
/// wrapper de linha da grid. Nenhum desses erros aparece em tempo de compilação; todos
/// estourariam na cara do operador na tela mais usada do módulo fiscal.
/// </summary>
public class NotaFiscalHubViewTests
{
    public NotaFiscalHubViewTests() => WpfTestBootstrap.Inicializar();

    private static NotaFiscalHubView CriarHub(ResumoNotasFiscaisDto? resumo = null)
    {
        var notaFiscalService = new Mock<INotaFiscalService>();
        notaFiscalService.Setup(s => s.ObterResumoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(resumo ?? new ResumoNotasFiscaisDto());

        var servicos = new ServiceCollection();
        servicos.AddSingleton(notaFiscalService.Object);
        var provider = servicos.BuildServiceProvider();

        return new NotaFiscalHubView(provider);
    }

    [StaFact]
    public void Hub_ConstroiComTodosOsElementosDoPainelDeEstatisticas()
    {
        var hub = CriarHub();

        foreach (var nome in new[]
                 {
                     "TxtTotalEmitidas", "TxtEmitidasHoje", "TxtValorTotalEmitido", "TxtValorEmitidoNoMes",
                     "TxtTicketMedio", "TxtTotalPendentes", "TxtTotalRejeitadas", "TxtTotalCanceladas",
                     "TxtContagemNFe", "TxtValorNFe", "TxtContagemNFCe", "TxtValorNFCe",
                     "ColBarraNFePreenchida", "ColBarraNFeVazia", "ColBarraNFCePreenchida", "ColBarraNFCeVazia",
                     "TxtStatusAutorizadas", "TxtStatusRejeitadas", "TxtStatusPendentes", "TxtStatusCanceladas",
                     "LinhaStatusDenegadas", "TxtStatusDenegadas",
                     "GridUltimasNotas", "TxtSemNotas", "PainelBarrasTipo", "TxtSemComparativoTipo"
                 })
        {
            Assert.True(hub.FindName(nome) is not null, $"'{nome}' não existe no XAML");
        }
    }

    /// <summary>A coluna Status virou um <c>DataGridTemplateColumn</c> com Border+TextBlock —
    /// se alguém voltar a uma <c>DataGridTextColumn</c> simples, perde a pílula colorida sem
    /// nenhum erro de compilação avisando.</summary>
    [StaFact]
    public void ColunaDeStatus_ContinuaSendoUmaPilulaColorida()
    {
        var hub = CriarHub();
        var grid = (DataGrid)hub.FindName("GridUltimasNotas")!;

        var colunaStatus = grid.Columns.Single(c => (c.Header as string) == "Status");
        Assert.IsType<DataGridTemplateColumn>(colunaStatus);
    }
}
