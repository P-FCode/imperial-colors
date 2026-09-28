using System.Windows;
using System.Windows.Controls;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Views;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Tela de controle das comissões. Construir a janela de verdade valida o XAML inteiro —
/// StaticResource que não resolve e binding para propriedade inexistente não aparecem em
/// tempo de compilação, e estourariam no primeiro clique do operador.
/// </summary>
public class ComissoesVendaExternaViewTests
{
    public ComissoesVendaExternaViewTests() => WpfTestBootstrap.Inicializar();

    private static ComissoesVendaExternaView Criar(params ComissaoVendaExternaDto[] comissoes)
    {
        var servico = new Mock<IVendaExternaService>();
        servico.Setup(s => s.ObterComissoesPaginadoAsync(
                It.IsAny<FiltroComissaoVendaExterna>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginacaoResultadoDto<ComissaoVendaExternaDto>
            {
                Itens = comissoes.ToList(),
                PaginaAtual = 1,
                ItensPorPagina = 50,
                TotalItens = comissoes.Length
            });
        servico.Setup(s => s.ObterResumoComissoesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResumoComissoesDto
            {
                TotalAPagar = comissoes.Where(c => !c.Paga).Sum(c => c.Comissao),
                QuantidadeAPagar = comissoes.Count(c => !c.Paga),
                TotalPago = comissoes.Where(c => c.Paga).Sum(c => c.Comissao),
                QuantidadePaga = comissoes.Count(c => c.Paga)
            });

        return new ComissoesVendaExternaView(servico.Object);
    }

    [StaFact]
    public void Tela_ConstroiComOsTotaisEAGradeDeComissoes()
    {
        var tela = Criar();

        foreach (var nome in new[]
                 {
                     "TxtTotalAPagar", "TxtQuantidadeAPagar", "TxtTotalPago", "TxtQuantidadePaga",
                     "TxtTotalDoMes", "GridComissoes", "TxtSemComissoes", "TxtResumoLista",
                     "RbAPagar", "RbPagas", "RbTodas", "BtnMarcarPaga", "BtnDesmarcar",
                     "BtnPaginaAnterior", "BtnPaginaProxima"
                 })
        {
            Assert.True(tela.FindName(nome) is not null, $"'{nome}' não existe no XAML");
        }

        tela.Close();
    }

    /// <summary>
    /// Os dois botões de acerto dependem da linha selecionada e da situação dela: marcar
    /// como paga o que já está pago, ou estornar o que nunca foi pago, não faz sentido. Sem
    /// seleção, nenhum dos dois pode estar disponível.
    /// </summary>
    [StaFact]
    public void SemLinhaSelecionada_OsBotoesDeAcertoFicamDesabilitados()
    {
        var tela = Criar();

        Assert.False(((Button)tela.FindName("BtnMarcarPaga")!).IsEnabled);
        Assert.False(((Button)tela.FindName("BtnDesmarcar")!).IsEnabled);

        tela.Close();
    }

    /// <summary>
    /// Antes da primeira carga a tela ainda não sabe quantas páginas existem, então os dois
    /// botões começam desabilitados — quem os habilita é o resultado da consulta. Habilitados
    /// de saída, um clique antes disso pediria uma página que pode não existir.
    /// </summary>
    [StaFact]
    public void AoAbrir_OsBotoesDeNavegacaoComecamDesabilitados()
    {
        var tela = Criar(new ComissaoVendaExternaDto
        {
            VendaExternaId = 1,
            NumeroVendaExterna = "VE-0001",
            DataVenda = DateTime.Today,
            TotalVenda = 160m,
            Comissao = 30m
        });

        Assert.False(((Button)tela.FindName("BtnPaginaAnterior")!).IsEnabled);
        Assert.False(((Button)tela.FindName("BtnPaginaProxima")!).IsEnabled);

        tela.Close();
    }

    /// <summary>O filtro começa em "A pagar": a pergunta que leva o lojista a abrir esta
    /// tela é quem ele ainda tem que pagar.</summary>
    [StaFact]
    public void AoAbrir_OFiltroComecaEmAPagar()
    {
        var tela = Criar();

        Assert.True(((RadioButton)tela.FindName("RbAPagar")!).IsChecked);
        Assert.False(((RadioButton)tela.FindName("RbPagas")!).IsChecked);

        tela.Close();
    }
}
