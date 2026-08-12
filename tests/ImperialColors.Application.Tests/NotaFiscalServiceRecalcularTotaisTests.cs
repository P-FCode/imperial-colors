using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// <c>RecalcularTotais</c> não toca nenhuma dependência injetada — é uma agregação pura
/// sobre os itens já calculados do DTO (bloco "Cálculo do imposto" da tela de emissão).
/// Todas as dependências do serviço são mockadas só para permitir a construção.
/// </summary>
public class NotaFiscalServiceRecalcularTotaisTests
{
    private static NotaFiscalService CriarServico() => new(
        Mock.Of<INotaFiscalRepository>(),
        Mock.Of<IVendaRepository>(),
        Mock.Of<IClienteRepository>(),
        Mock.Of<IProdutoRepository>(),
        Mock.Of<ITributacaoProdutoRepository>(),
        Mock.Of<ITributacaoCategoriaRepository>(),
        Mock.Of<IRepository<NaturezaOperacao>>(),
        Mock.Of<IConfiguracaoFiscalService>(),
        Mock.Of<IFiscalApiClient>());

    private static ItemNotaFiscalDto Item(decimal total, decimal? icms = null, decimal? ibsUf = null, decimal? ibsMun = null, decimal? cbs = null, bool compoeTotal = true) => new()
    {
        ValorTotal = total,
        BaseIcms = icms is > 0 ? total : null,
        ValorIcms = icms,
        ValorIbsUf = ibsUf,
        ValorIbsMunicipio = ibsMun,
        ValorCbs = cbs,
        BaseIbsCbs = total,
        CompoeTotalNota = compoeTotal
    };

    [Fact]
    public void RecalcularTotais_SomaValorDosItensQueCompoemOTotal()
    {
        var servico = CriarServico();
        var nota = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            Itens = new List<ItemNotaFiscalDto>
            {
                Item(100m, icms: 18m, ibsUf: 0.10m, ibsMun: 0m, cbs: 0.90m),
                Item(50m, icms: 9m, ibsUf: 0.05m, ibsMun: 0m, cbs: 0.45m),
                Item(30m, compoeTotal: false) // não deve entrar no vProd
            }
        };

        var resultado = servico.RecalcularTotais(nota);

        Assert.Equal(150m, resultado.VProd); // 100 + 50, item fora do total não conta
        Assert.Equal(27m, resultado.VIcms); // 18 + 9
        Assert.Equal(0.15m, resultado.VIbsUf); // 0.10 + 0.05
        Assert.Equal(0m, resultado.VIbsMunicipio);
        Assert.Equal(0.15m, resultado.VIbs); // VIbsUf + VIbsMunicipio
        Assert.Equal(1.35m, resultado.VCbs); // 0.90 + 0.45
        Assert.Equal(3, resultado.NumeroItens);
        Assert.Equal(1, resultado.Itens[0].NItem);
        Assert.Equal(2, resultado.Itens[1].NItem);
        Assert.Equal(3, resultado.Itens[2].NItem);
    }

    [Fact]
    public void RecalcularTotais_VNfConsideraFreteSeguroDespesasEDesconto()
    {
        var servico = CriarServico();
        var nota = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            VFrete = 10m,
            VSeg = 5m,
            VOutro = 2m,
            VDesc = 20m,
            Itens = new List<ItemNotaFiscalDto> { Item(100m) }
        };

        var resultado = servico.RecalcularTotais(nota);

        // vNf = vProd + vST + vServ + vFrete + vSeg + vOutro + vIpi - vDesc
        Assert.Equal(100m + 10m + 5m + 2m - 20m, resultado.VNf);
    }

    /// <summary>
    /// Regressão: vNf omitia o ICMS-ST da fórmula (GUIA_INTEGRACAO.md seção 4.8 exige
    /// vNF = vProd + vST + vFrete + vSeg + vOutro + vIPI − vDesc), subestimando o total
    /// persistido/exibido sempre que havia ICMS-ST — mesmo com o XML transmitido correto
    /// (NotaFiscalPayloadBuilder recalcula à parte a partir dos itens).
    /// </summary>
    [Fact]
    public void RecalcularTotais_VNfIncluiIcmsSt()
    {
        var servico = CriarServico();
        var nota = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            Itens = new List<ItemNotaFiscalDto>
            {
                new() { ValorTotal = 100m, CompoeTotalNota = true, BaseIcmsSt = 100m, ValorIcmsSt = 15m }
            }
        };

        var resultado = servico.RecalcularTotais(nota);

        Assert.Equal(15m, resultado.VIcmsSt);
        Assert.Equal(115m, resultado.VNf); // 100 (vProd) + 15 (vST)
    }

    [Fact]
    public void RecalcularTotais_CalculoAutomaticoDesligado_NaoAlteraNota()
    {
        var servico = CriarServico();
        var nota = new NotaFiscalDto
        {
            CalculoAutomatico = false,
            VProd = 999m,
            Itens = new List<ItemNotaFiscalDto> { Item(100m) }
        };

        var resultado = servico.RecalcularTotais(nota);

        Assert.Equal(999m, resultado.VProd); // preservado — o operador controla manualmente
    }
}
