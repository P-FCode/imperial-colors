using ImperialColors.Application.DTOs;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

public class NotaFiscalValidatorTests
{
    private static ItemNotaFiscalDto ItemValido() => new()
    {
        Descricao = "Tinta Coral 18L",
        Ncm = "32091019",
        Cfop = "5102",
        CsosnIcms = "102",
        CstIbsCbs = "000",
        CClassTrib = "000001",
        Quantidade = 1,
        ValorUnitario = 100m,
        ValorTotal = 100m,
        CompoeTotalNota = true
    };

    private static NotaFiscalDto NotaValida() => new()
    {
        Serie = "1",
        Numero = "1001",
        VNf = 100m,
        DestinatarioDocumento = "00000000000191",
        DestinatarioIndicadorIe = IndicadorIeDestinatario.NaoContribuinte,
        Itens = new List<ItemNotaFiscalDto> { ItemValido() },
        Pagamentos = new List<NotaFiscalPagamentoDto>
        {
            new() { FormaPagamento = FormaPagamento.Dinheiro, Valor = 100m }
        }
    };

    [Fact]
    public void ValidarParaEmissao_NotaValida_NaoLancaExcecao()
    {
        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(NotaValida(), "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_SemItens_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens.Clear();

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("item", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarParaEmissao_ItemSemNcmComValidarNcmLigado_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens[0].Ncm = null;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("NCM", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_ItemSemNcmComValidarNcmDesligado_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].Ncm = null;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: false));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_CfopDentroEstadoComPrefixo6_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens[0].Cfop = "6102";

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CFOP", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_VendaInterestadualComCfopDentroEstado_LancaDomainException()
    {
        var nota = NotaValida();
        nota.DestinatarioUf = "SP"; // emitente é PR — venda interestadual
        nota.Itens[0].Cfop = "5102"; // deveria ser 6xxx

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CFOP", ex.Message);
    }

    /// <summary>
    /// Regressão: o validador real da API (FiscalLayoutValidator) aceita 1xxx (entrada/
    /// devolução) além de 5xxx para operação dentro do estado — restringir só a 5xxx
    /// bloquearia localmente uma nota de devolução legítima antes de chegar à API.
    /// </summary>
    [Fact]
    public void ValidarParaEmissao_CfopDentroEstadoComPrefixo1_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].Cfop = "1102";

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_VendaInterestadualComCfopPrefixo2_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.DestinatarioUf = "SP";
        nota.Itens[0].Cfop = "2102";

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_CstECsosnPreenchidosJuntos_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens[0].CstIcms = "00";
        nota.Itens[0].CsosnIcms = "102";

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CST ou apenas CSOSN", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_ItemSemCstIbsCbs_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens[0].CstIbsCbs = null;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("IBS/CBS", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_SomaPagamentosDivergeDoTotal_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Pagamentos[0].Valor = 50m; // total é 100

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("pagamentos", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidarParaEmissao_SemPagamento_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Pagamentos.Clear();

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("pagamento", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ===== Forma de pagamento "Sem Pagamento" (tPag=90) — regressão =====

    [Fact]
    public void ValidarParaEmissao_FormaSemPagamentoSozinha_NaoLancaExcecaoMesmoComSomaDivergente()
    {
        var nota = NotaValida();
        nota.Pagamentos = new List<NotaFiscalPagamentoDto>
        {
            new() { FormaPagamento = FormaPagamento.SemPagamento, Valor = 0m }
        };
        // nota.VNf = 100m (herdado de NotaValida) — soma dos pagamentos (0) diverge, mas
        // "Sem Pagamento" não deve exigir que bata com o total, já que não há valor real.

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_FormaSemPagamentoCombinadaComOutraForma_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Pagamentos = new List<NotaFiscalPagamentoDto>
        {
            new() { FormaPagamento = FormaPagamento.SemPagamento, Valor = 0m },
            new() { FormaPagamento = FormaPagamento.Dinheiro, Valor = 100m }
        };

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("Sem Pagamento", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("curta demais")]
    public void ValidarJustificativaCancelamento_MenorQue15Caracteres_LancaDomainException(string? justificativa)
    {
        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarJustificativaCancelamento(justificativa));
        Assert.Contains("15 caracteres", ex.Message);
    }

    [Fact]
    public void ValidarJustificativaCancelamento_ComPeloMenos15Caracteres_NaoLancaExcecao()
    {
        var ex = Record.Exception(() => NotaFiscalValidator.ValidarJustificativaCancelamento("Cancelamento por erro de digitacao no pedido"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("Corrigir o VALOR do produto informado incorretamente")]
    [InlineData("Corrigir o nome do DESTINATARIO da nota fiscal")]
    [InlineData("Ajustar o IMPOSTO calculado incorretamente na nota")]
    [InlineData("Corrigir o PRECO informado na nota fiscal emitida")]
    public void ValidarCorrecao_ComPalavraProibida_LancaDomainException(string correcao)
    {
        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarCorrecao(correcao));
        Assert.Contains("VALOR, DESTINATARIO, IMPOSTO ou PRECO", ex.Message);
    }

    [Fact]
    public void ValidarCorrecao_TextoValido_NaoLancaExcecao()
    {
        var ex = Record.Exception(() => NotaFiscalValidator.ValidarCorrecao("Corrigindo o nome do transportador informado nas observacoes"));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarInutilizacao_NumeroInicialMaiorQueFinal_LancaDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            NotaFiscalValidator.ValidarInutilizacao("Falha no sistema de numeracao sequencial", "105", "100"));
        Assert.Contains("maior que o final", ex.Message);
    }

    [Fact]
    public void ValidarInutilizacao_ParametrosValidos_NaoLancaExcecao()
    {
        var ex = Record.Exception(() =>
            NotaFiscalValidator.ValidarInutilizacao("Falha no sistema de numeracao sequencial", "100", "105"));
        Assert.Null(ex);
    }
}
