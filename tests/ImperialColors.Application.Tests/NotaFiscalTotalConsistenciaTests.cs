using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Fiscal;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Trava a consistência entre o <c>VNf</c> calculado na Application
/// (<c>NotaFiscalService.RecalcularTotais</c>, que é o número persistido, exibido na tela e
/// conferido contra a soma dos pagamentos por <c>NotaFiscalValidator</c>) e o
/// <c>total.ICMSTot.vNF</c> que <see cref="NotaFiscalPayloadBuilder"/> efetivamente transmite.
///
/// As duas fórmulas são propositalmente diferentes — a Application soma campos de nota, o
/// builder soma os itens realmente montados no payload — e por isso divergiam em silêncio:
/// enquanto existiu um campo "Total Serviços" na tela, <c>vServ</c> entrava só no primeiro
/// lado e a nota saía com <c>vNF</c> menor que a soma dos pagamentos declarados, sendo
/// rejeitada pela SEFAZ. Estes testes existem para que qualquer nova parcela adicionada a um
/// dos lados e esquecida no outro quebre o build em vez de virar rejeição em produção.
/// </summary>
public class NotaFiscalTotalConsistenciaTests
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

    private static EmitenteFiscalDto Emitente() => new()
    {
        Cnpj = "13416624000136",
        RazaoSocial = "Imperial Colors Tintas Ltda",
        InscricaoEstadual = "1234567891",
        Crt = "3",
        Logradouro = "Avenida Central",
        Numero = "1000",
        Bairro = "Centro",
        CodigoMunicipioIbge = "4106902",
        Municipio = "Curitiba",
        Uf = "PR",
        Cep = "80530000"
    };

    /// <summary>Item com ICMS próprio; opcionalmente com ICMS-ST "para frente" (CST 10).</summary>
    private static ItemNotaFiscalDto ItemDto(decimal total, decimal? icmsSt = null) => new()
    {
        NItem = 1,
        CodigoProduto = "P001",
        Descricao = "Tinta Coral 18L",
        Ncm = "32091019",
        Cfop = "5102",
        Unidade = "UN",
        Quantidade = 1m,
        ValorUnitario = total,
        ValorTotal = total,
        CompoeTotalNota = true,
        Origem = "0",
        CstIcms = icmsSt.HasValue ? "10" : "00",
        BaseIcms = total,
        AliquotaIcms = 18m,
        ValorIcms = Math.Round(total * 0.18m, 2),
        BaseIcmsSt = icmsSt.HasValue ? total * 1.4m : null,
        Mva = icmsSt.HasValue ? 40m : null,
        AliquotaIcmsSt = icmsSt.HasValue ? 18m : null,
        ValorIcmsSt = icmsSt,
        CstPis = "01",
        CstCofins = "01",
        CstIbsCbs = "000",
        CClassTrib = "000001",
        BaseIbsCbs = total
    };

    private static ItemNotaFiscal ParaEntidade(ItemNotaFiscalDto d) => new()
    {
        NItem = d.NItem,
        CodigoProduto = d.CodigoProduto,
        Descricao = d.Descricao,
        Ncm = d.Ncm,
        Cfop = d.Cfop,
        Unidade = d.Unidade,
        Quantidade = d.Quantidade,
        ValorUnitario = d.ValorUnitario,
        ValorTotal = d.ValorTotal,
        CompoeTotalNota = d.CompoeTotalNota,
        Origem = d.Origem,
        CstIcms = d.CstIcms,
        BaseIcms = d.BaseIcms,
        AliquotaIcms = d.AliquotaIcms,
        ValorIcms = d.ValorIcms,
        BaseIcmsSt = d.BaseIcmsSt,
        Mva = d.Mva,
        AliquotaIcmsSt = d.AliquotaIcmsSt,
        ValorIcmsSt = d.ValorIcmsSt,
        CstPis = d.CstPis,
        CstCofins = d.CstCofins,
        CstIbsCbs = d.CstIbsCbs,
        CClassTrib = d.CClassTrib,
        BaseIbsCbs = d.BaseIbsCbs
    };

    /// <summary>Roda o cálculo da Application e o do payload sobre a MESMA nota e devolve os dois vNF.</summary>
    private static (decimal VNfApplication, decimal VNfPayload) CalcularPelosDoisCaminhos(NotaFiscalDto dto)
    {
        var recalculada = CriarServico().RecalcularTotais(dto);

        var entidade = new NotaFiscal
        {
            Tipo = TipoNotaFiscal.NFe,
            Serie = "1",
            Numero = "1001",
            DataEmissao = new DateTime(2026, 8, 10, 9, 0, 0),
            NaturezaOperacaoDescricao = "VENDA DE MERCADORIA",
            Crt = "3",
            Ambiente = AmbienteEmissaoFiscal.Producao,
            DestinatarioNome = "Fulano de Tal",
            DestinatarioTipoPessoa = TipoPessoa.Fisica,
            DestinatarioDocumento = "11144477735",
            DestinatarioIndicadorIe = IndicadorIeDestinatario.NaoContribuinte,
            DestinatarioUf = "PR",
            VProd = recalculada.VProd,
            VFrete = recalculada.VFrete,
            VSeg = recalculada.VSeg,
            VDesc = recalculada.VDesc,
            VOutro = recalculada.VOutro,
            VIcmsSt = recalculada.VIcmsSt,
            VBcIcms = recalculada.VBcIcms,
            VIcms = recalculada.VIcms,
            VIpi = recalculada.VIpi,
            VNf = recalculada.VNf,
            Itens = recalculada.Itens.Select(ParaEntidade).ToList()
        };

        var payload = NotaFiscalPayloadBuilder.Construir(entidade, Emitente());
        return (recalculada.VNf, payload.InfNFe.Total.ICMSTot.VNF);
    }

    [Fact]
    public void NotaSimples_TotalDaApplicationBateComOTransmitido()
    {
        var dto = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            Itens = [ItemDto(250m)]
        };

        var (aplicacao, payload) = CalcularPelosDoisCaminhos(dto);

        Assert.Equal(250m, aplicacao);
        Assert.Equal(aplicacao, payload);
    }

    /// <summary>
    /// Regressão do bug real: o ICMS-ST viaja embutido em <c>prod.vOutro</c> do primeiro item
    /// (não existe campo de ST por item no schema REST), enquanto a Application o soma como
    /// parcela separada. Os dois caminhos precisam chegar no mesmo número.
    /// </summary>
    [Fact]
    public void NotaComIcmsSt_TotalDaApplicationBateComOTransmitido()
    {
        var dto = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            Itens = [ItemDto(250m, icmsSt: 63m)]
        };

        var (aplicacao, payload) = CalcularPelosDoisCaminhos(dto);

        Assert.Equal(313m, aplicacao); // 250 (vProd) + 63 (vST)
        Assert.Equal(aplicacao, payload);
    }

    [Fact]
    public void NotaComFreteSeguroDescontoEDespesas_TotalDaApplicationBateComOTransmitido()
    {
        var dto = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            VFrete = 40m,
            VSeg = 15m,
            VOutro = 7m,
            VDesc = 22m,
            Itens = [ItemDto(250m, icmsSt: 63m)]
        };

        var (aplicacao, payload) = CalcularPelosDoisCaminhos(dto);

        Assert.Equal(250m + 63m + 40m + 15m + 7m - 22m, aplicacao);
        Assert.Equal(aplicacao, payload);
    }

    /// <summary>
    /// <c>vServ</c> não tem grupo correspondente no payload de NF-e/NFC-e (ISSQN é da NFS-e).
    /// Se alguém voltar a somá-lo em <c>RecalcularTotais</c>, os dois lados divergem e este
    /// teste quebra — que é exatamente o sintoma que a SEFAZ devolveria como rejeição.
    /// </summary>
    [Fact]
    public void VServPreenchido_NaoInflaOTotalENaoQuebraAConsistencia()
    {
        var dto = new NotaFiscalDto
        {
            CalculoAutomatico = true,
            VServ = 500m,
            Itens = [ItemDto(250m)]
        };

        var (aplicacao, payload) = CalcularPelosDoisCaminhos(dto);

        Assert.Equal(250m, aplicacao); // vServ ignorado
        Assert.Equal(aplicacao, payload);
    }
}
