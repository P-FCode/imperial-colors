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
        // Endereço completo: o grupo enderDest é obrigatório no leiaute 4.00 sempre que a
        // NF-e tem destinatário, então uma nota sem ele não é "válida" — a fixture precisa
        // refletir isso para os testes exercitarem o caminho real de emissão.
        DestinatarioLogradouro = "Rua das Tintas",
        DestinatarioNumero = "100",
        DestinatarioBairro = "Centro",
        DestinatarioCidade = "Curitiba",
        DestinatarioCodigoMunicipioIbge = "4106902",
        DestinatarioUf = "PR",
        DestinatarioCep = "80530000",
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

    /// <summary>
    /// NCM é obrigatório no leiaute em qualquer cenário — a opção "Validar NCM em notas"
    /// controla a conferência do FORMATO, não a existência do campo. Com ela desligada o
    /// item seguia sem NCM e o XML saía com a tag vazia, rejeitado pelo XSD.
    /// </summary>
    [Fact]
    public void ValidarParaEmissao_ItemSemNcm_LancaDomainExceptionMesmoComValidacaoDesligada()
    {
        var nota = NotaValida();
        nota.Itens[0].Ncm = null;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: false));
        Assert.Contains("NCM", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_NcmComFormatoInvalidoEValidacaoLigada_LancaDomainException()
    {
        var nota = NotaValida();
        nota.Itens[0].Ncm = "3209"; // 4 dígitos — o leiaute exige 8

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("8 dígitos", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_NcmComFormatoInvalidoEValidacaoDesligada_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].Ncm = "3209";

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

    // ===== ICMS 00/20/51/90 sem alíquota cadastrada — regressão do XSD_VALIDATION
    // "incomplete content... expected 'pICMS'" (grupo ICMS00 sai sem vBC/pICMS/vICMS) =====

    [Theory]
    [InlineData("00")]
    [InlineData("20")]
    [InlineData("51")]
    [InlineData("90")]
    public void ValidarParaEmissao_CstTributacaoIntegralSemAliquotaCadastrada_LancaDomainException(string cst)
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = cst;
        nota.Itens[0].AliquotaIcms = null;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("exige alíquota", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_Cst00ComAliquotaCadastrada_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = "00";
        nota.Itens[0].AliquotaIcms = 18m;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    // ===== CRT × CST/CSOSN — o par errado é rejeitado pela SEFAZ (ela espera o grupo
    // ICMSSN quando o CRT é do Simples) e acontecia sozinho quando a Regra Geral da
    // empresa tinha CST cadastrado com a empresa no Simples Nacional. =====

    [Theory]
    [InlineData("1")]
    [InlineData("4")]
    public void ValidarParaEmissao_CrtDoSimplesComCstNoItem_LancaDomainException(string crt)
    {
        var nota = NotaValida();
        nota.Crt = crt;
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = "00";
        nota.Itens[0].AliquotaIcms = 18m;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CSOSN", ex.Message);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    public void ValidarParaEmissao_CrtDoRegimeNormalComCsosnNoItem_LancaDomainException(string crt)
    {
        var nota = NotaValida(); // ItemValido() usa CSOSN 102
        nota.Crt = crt;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CST", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_CrtDoSimplesComCsosnNoItem_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Crt = "1";

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    // ===== ICMS-ST: bloqueia só quando FALTA o cadastro necessário, não o código em si
    // (desde que CalculoFiscalHelper passou a calcular ST de verdade — CST 10/CSOSN
    // 201/202/203 via MVA, CST 60/CSOSN 500 via pST informado). =====

    [Theory]
    [InlineData("10")]
    [InlineData("60")]
    public void ValidarParaEmissao_CstStSemCadastroNecessario_LancaDomainException(string cst)
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = cst;
        nota.Itens[0].AliquotaIcms = 18m; // alíquota própria presente, mas falta MVA/AliquotaIcmsSt (10) ou pST (60)

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("Substituição Tributária", ex.Message);
    }

    /// <summary>Regressão: nota rejeitada pela SEFAZ ("Nao informada vBCSTRet, pST e
    /// vICMSSTRet") porque um item com CSOSN 500 passava pela validação sem bloqueio — só o
    /// CST 10/60 (mesma limitação, lado Regime Normal) era bloqueado.</summary>
    [Theory]
    [InlineData("201")]
    [InlineData("202")]
    [InlineData("203")]
    [InlineData("500")]
    public void ValidarParaEmissao_CsosnStSemCadastroNecessario_LancaDomainException(string csosn)
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = csosn;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("Substituição Tributária", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_Cst10ComMvaEAliquotaSt_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = "10";
        nota.Itens[0].AliquotaIcms = 18m;
        nota.Itens[0].Mva = 40m;
        nota.Itens[0].AliquotaIcmsSt = 18m;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_Cst60ComAliquotaStRetido_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = null;
        nota.Itens[0].CstIcms = "60";
        nota.Itens[0].AliquotaIcmsStRetido = 12m;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_Csosn201ComMvaEAliquotaSt_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = "201";
        nota.Itens[0].Mva = 40m;
        nota.Itens[0].AliquotaIcmsSt = 18m;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_Csosn500ComAliquotaStRetido_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Itens[0].CsosnIcms = "500";
        nota.Itens[0].AliquotaIcmsStRetido = 12m;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
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

    // ===== Documento do destinatário (dígito verificador) =====
    // Sem estas conferências, um dígito trocado só aparecia como rejeição da SEFAZ
    // (cStat 207/209) — que consome o número da nota, já que numeração não volta pra fila.

    [Fact]
    public void ValidarParaEmissao_CnpjDoDestinatarioComDigitoErrado_LancaDomainException()
    {
        var nota = NotaValida();
        nota.DestinatarioDocumento = "00000000000192"; // BB com o último dígito trocado

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CNPJ", ex.Message);
        Assert.Contains("inválido", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_CpfDoDestinatarioComDigitoErrado_LancaDomainException()
    {
        var nota = NotaValida();
        nota.DestinatarioDocumento = "11144477734"; // válido seria ...35

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("CPF", ex.Message);
    }

    [Fact]
    public void ValidarParaEmissao_CpfValidoNoDestinatario_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.DestinatarioDocumento = "111.444.777-35"; // com máscara, deve ser normalizado

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    [Fact]
    public void ValidarParaEmissao_DocumentoComTamanhoInvalido_LancaDomainException()
    {
        var nota = NotaValida();
        nota.DestinatarioDocumento = "123456789"; // nem CPF (11) nem CNPJ (14)

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Contains("9 dígito", ex.Message);
    }

    // ===== Endereço do destinatário (grupo enderDest) =====

    /// <summary>
    /// Regressão: NotaFiscalPayloadBuilder.ConstruirDest só monta enderDest quando há
    /// logradouro. Sem esta validação a NF-e saía sem o grupo e a SEFAZ derrubava por XSD.
    /// </summary>
    [Fact]
    public void ValidarParaEmissao_NfeSemEnderecoDoDestinatario_LancaDomainExceptionListandoOsCampos()
    {
        var nota = NotaValida();
        nota.Tipo = TipoNotaFiscal.NFe;
        nota.DestinatarioLogradouro = null;
        nota.DestinatarioBairro = null;
        nota.DestinatarioCodigoMunicipioIbge = null;

        var ex = Assert.Throws<DomainException>(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));

        // Confere só o trecho "Faltando: ..." — o restante da mensagem é orientação ao
        // operador e cita CEP/IBGE de propósito ("a busca por CEP preenche tudo").
        var listaFaltando = ex.Message.Split("Faltando:")[1].Split('.')[0];
        Assert.Contains("Logradouro", listaFaltando);
        Assert.Contains("Bairro", listaFaltando);
        Assert.Contains("Código IBGE", listaFaltando);
        // Os que estão preenchidos não entram na lista.
        Assert.DoesNotContain("CEP", listaFaltando);
        Assert.DoesNotContain("UF", listaFaltando);
    }

    /// <summary>
    /// Na NFC-e o destinatário é opcional e, quando informado (cupom com CPF), o endereço
    /// não é exigido — é venda de balcão. Exigir aqui travaria o PDV.
    /// </summary>
    [Fact]
    public void ValidarParaEmissao_NfceSemEnderecoDoDestinatario_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Tipo = TipoNotaFiscal.NFCe;
        nota.DestinatarioLogradouro = null;
        nota.DestinatarioBairro = null;
        nota.DestinatarioCidade = null;
        nota.DestinatarioCodigoMunicipioIbge = null;
        nota.DestinatarioUf = null;
        nota.DestinatarioCep = null;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }

    /// <summary>NFC-e de balcão sem cliente nenhum: não há dest, então não há endereço a exigir.</summary>
    [Fact]
    public void ValidarParaEmissao_NfceSemDestinatarioAlgum_NaoLancaExcecao()
    {
        var nota = NotaValida();
        nota.Tipo = TipoNotaFiscal.NFCe;
        nota.DestinatarioDocumento = null;
        nota.DestinatarioNome = null;
        nota.DestinatarioLogradouro = null;
        nota.DestinatarioBairro = null;
        nota.DestinatarioCidade = null;
        nota.DestinatarioCodigoMunicipioIbge = null;
        nota.DestinatarioUf = null;
        nota.DestinatarioCep = null;

        var ex = Record.Exception(() => NotaFiscalValidator.ValidarParaEmissao(nota, "PR", validarNcm: true));
        Assert.Null(ex);
    }
}
