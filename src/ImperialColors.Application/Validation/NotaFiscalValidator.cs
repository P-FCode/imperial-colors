using System.Text.RegularExpressions;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Validation;

/// <summary>
/// Valida uma <see cref="NotaFiscalDto"/> antes de emitir — checa localmente tudo que dá
/// pra pegar antes de gastar uma chamada com a API Fiscal (CFOP coerente, CST×CSOSN
/// mutuamente exclusivos, NCM obrigatório, IBS/CBS presente em todo item — regra 2026 do
/// GUIA_INTEGRACAO.md seção 4.10 — e pagamentos batendo com o total).
/// </summary>
public static class NotaFiscalValidator
{
    private static readonly Regex ApenasDigitos = new("^[0-9]+$", RegexOptions.Compiled);
    private const string TextoProibidoCorrecao = "não pode conter VALOR, DESTINATARIO, IMPOSTO ou PRECO — a legislação proíbe corrigir base de cálculo, valores, impostos ou destinatário por CC-e.";

    public static void ValidarParaEmissao(NotaFiscalDto nota, string ufEmitente, bool validarNcm)
    {
        if (string.IsNullOrWhiteSpace(nota.Serie))
            throw new DomainException("Série é obrigatória.");

        if (string.IsNullOrWhiteSpace(nota.Numero))
            throw new DomainException("Número da nota é obrigatório.");

        if (nota.Itens.Count == 0)
            throw new DomainException("A nota precisa ter ao menos um item.");

        var interestadual = !string.IsNullOrWhiteSpace(nota.DestinatarioUf) &&
            !string.Equals(nota.DestinatarioUf, ufEmitente, StringComparison.OrdinalIgnoreCase);

        var indice = 0;
        foreach (var item in nota.Itens)
        {
            indice++;
            var rotulo = $"Item {indice} ('{item.Descricao}')";

            // NCM é obrigatório no leiaute da NF-e/NFC-e em qualquer cenário — a opção
            // "Validar NCM em notas" controla a conferência do FORMATO (8 dígitos), não a
            // existência do campo. Antes, com a opção desligada, o item seguia sem NCM e o
            // XML saía com <NCM></NCM>, rejeitado pelo XSD.
            if (string.IsNullOrWhiteSpace(item.Ncm))
                throw new DomainException(
                    $"{rotulo}: NCM é obrigatório no XML — cadastre o NCM do produto (Estoque → editar produto → aba Tributação) antes de emitir.");

            if (validarNcm && (item.Ncm.Trim().Length != 8 || !ApenasDigitos.IsMatch(item.Ncm.Trim())))
                throw new DomainException(
                    $"{rotulo}: NCM '{item.Ncm}' inválido — deve ter exatamente 8 dígitos numéricos (Configurações → Fiscal → Validar NCM em notas está ligado).");

            ValidarCfop(item.Cfop, interestadual, rotulo);
            ValidarIcmsItem(item, nota.Crt, rotulo);

            if (string.IsNullOrWhiteSpace(item.CstIbsCbs) || string.IsNullOrWhiteSpace(item.CClassTrib))
                throw new DomainException(
                    $"{rotulo}: CST e cClassTrib do IBS/CBS são obrigatórios em todo item desde a Reforma Tributária (2026) — a SEFAZ rejeita com cStat 1115 se faltar.");
        }

        if (nota.DestinatarioIndicadorIe is null &&
            string.IsNullOrWhiteSpace(nota.DestinatarioDocumento) &&
            nota.Tipo == Domain.Enums.TipoNotaFiscal.NFe)
            throw new DomainException("NF-e exige destinatário — NFC-e é a única que permite venda sem cliente identificado.");

        if (nota.Pagamentos.Count == 0)
            throw new DomainException("Informe ao menos uma forma de pagamento.");

        // "Sem Pagamento" (tPag=90) é pra nota sem venda de verdade por trás (transferência,
        // amostra, brinde) — não faz sentido exigir que "bata com o total" (não há valor
        // real recebido), nem misturar com uma forma de pagamento de verdade na mesma nota.
        var temSemPagamento = nota.Pagamentos.Any(p => p.FormaPagamento == FormaPagamento.SemPagamento);
        if (temSemPagamento)
        {
            if (nota.Pagamentos.Count > 1)
                throw new DomainException(
                    "'Sem Pagamento' não pode ser combinado com outra forma de pagamento na mesma nota — use um ou outro.");
            return;
        }

        var somaPagamentos = nota.Pagamentos.Sum(p => p.Valor);
        if (Math.Abs(somaPagamentos - nota.VNf) > 0.01m)
            throw new DomainException(
                $"A soma dos pagamentos (R$ {somaPagamentos:0.00}) não bate com o total da nota (R$ {nota.VNf:0.00}).");
    }

    private static void ValidarIcmsItem(ItemNotaFiscalDto item, string? crt, string rotulo)
    {
        var temCst = !string.IsNullOrWhiteSpace(item.CstIcms);
        var temCsosn = !string.IsNullOrWhiteSpace(item.CsosnIcms);

        if (temCst && temCsosn)
            throw new DomainException($"{rotulo}: preencha apenas CST ou apenas CSOSN do ICMS, nunca os dois.");

        if (!temCst && !temCsosn)
            throw new DomainException($"{rotulo}: falta CST ou CSOSN do ICMS.");

        // CRT 1 (Simples Nacional) e 4 (MEI) usam CSOSN; CRT 2 (Simples com excesso de
        // sublimite) e 3 (Regime Normal) usam CST. Mandar o par errado é rejeição certa da
        // SEFAZ (ela espera o grupo ICMSSN quando o CRT é do Simples) — e acontecia sozinho
        // quando a Regra Geral da empresa tinha CST cadastrado com a empresa no Simples.
        if (crt is "1" or "4" && temCst)
            throw new DomainException(
                $"{rotulo}: a empresa está no Simples Nacional/MEI (CRT {crt}), que exige CSOSN — este item está com CST de ICMS '{item.CstIcms}'. Corrija a tributação do produto ou a Regra Geral em Configurações → Fiscal.");

        if (crt is "2" or "3" && temCsosn)
            throw new DomainException(
                $"{rotulo}: a empresa está no Regime Normal (CRT {crt}), que exige CST — este item está com CSOSN '{item.CsosnIcms}'. Corrija a tributação do produto ou a Regra Geral em Configurações → Fiscal.");

        if (!temCst)
        {
            // CSOSN 201/202/203 (ICMSSN201/202/203 — ST "para frente") e 500 (ICMSSN500 — ST
            // retida anteriormente) EXIGEM campos extras que só existem se o cadastro do
            // produto os tiver — o cálculo real está em CalculoFiscalHelper desde a correção
            // da rejeição "Nao informada vBCSTRet, pST e vICMSSTRet". Valida aqui a PRESENÇA
            // dos campos, não mais um bloqueio cego do código inteiro.
            if (CalculoFiscalHelper.CsosnStParaFrente.Contains(item.CsosnIcms!) &&
                (item.Mva is null || item.AliquotaIcmsSt is null))
                throw new DomainException(
                    $"{rotulo}: CSOSN '{item.CsosnIcms}' é de Substituição Tributária — exige MVA/IVA-ST e Alíquota de ICMS-ST cadastrados no produto (Estoque → editar produto → aba Tributação) para calcular vBCST/vICMSST.");

            if (CalculoFiscalHelper.CsosnStRetido.Contains(item.CsosnIcms!) && item.AliquotaIcmsStRetido is null)
                throw new DomainException(
                    $"{rotulo}: CSOSN '{item.CsosnIcms}' é de Substituição Tributária retida — exige a Alíquota de ICMS-ST Retido (pST) cadastrada no produto (Estoque → editar produto → aba Tributação).");

            return;
        }

        // CST 00/20/51/90 (ICMS00/20/51/90 do leiaute NFe) e CST 10 (que também tem grupo
        // próprio) exigem vBC/pICMS/vICMS no XML — sem alíquota cadastrada esses campos saem
        // nulos e a SEFAZ rejeita por XSD_VALIDATION ("incomplete content... expected
        // 'pICMS'"). Bloquear aqui evita gastar uma chamada com a API pra descobrir isso.
        if ((CalculoFiscalHelper.CstIcmsTributacaoIntegral.Contains(item.CstIcms!) ||
             CalculoFiscalHelper.CstIcmsStParaFrente.Contains(item.CstIcms!)) && item.AliquotaIcms is null)
            throw new DomainException(
                $"{rotulo}: CST de ICMS '{item.CstIcms}' exige alíquota — cadastre em Estoque → editar produto → aba Tributação (ou, se este produto usa a Regra Geral da empresa, em Configurações → Fiscal → Alíquota de ICMS padrão). Depois clique em 'Atualizar' na linha do item: a nota guarda uma cópia dos dados fiscais do momento em que o item foi adicionado e não acompanha sozinha as mudanças no cadastro.");

        // CST 10 — Substituição Tributária "para frente": além da alíquota própria (conferida
        // acima), exige MVA e Alíquota de ICMS-ST para montar o grupo vBCST/pICMSST/vICMSST.
        if (CalculoFiscalHelper.CstIcmsStParaFrente.Contains(item.CstIcms!) &&
            (item.Mva is null || item.AliquotaIcmsSt is null))
            throw new DomainException(
                $"{rotulo}: CST de ICMS '{item.CstIcms}' é de Substituição Tributária — exige MVA/IVA-ST e Alíquota de ICMS-ST cadastrados no produto (Estoque → editar produto → aba Tributação) para calcular vBCST/vICMSST.");

        // CST 60 — ICMS-ST retido anteriormente: exige a alíquota retida (pST) informada no
        // cadastro; não há fórmula a calcular, é um valor declarado.
        if (CalculoFiscalHelper.CstIcmsStRetido.Contains(item.CstIcms!) && item.AliquotaIcmsStRetido is null)
            throw new DomainException(
                $"{rotulo}: CST de ICMS '{item.CstIcms}' é de Substituição Tributária retida — exige a Alíquota de ICMS-ST Retido (pST) cadastrada no produto (Estoque → editar produto → aba Tributação).");
    }

    private static void ValidarCfop(string? cfop, bool interestadual, string rotulo)
    {
        if (string.IsNullOrWhiteSpace(cfop) || cfop.Length != 4 || !ApenasDigitos.IsMatch(cfop))
            throw new DomainException($"{rotulo}: CFOP deve conter exatamente 4 dígitos numéricos.");

        // A API/validador real (FiscalLayoutValidator) aceita 1xxx/5xxx para operação interna
        // e 2xxx/6xxx para interestadual — 1xxx/2xxx são operações de entrada/devolução
        // (FinalidadeNfe.Devolucao já existe no domínio). Restringir só a 5xxx/6xxx bloquearia
        // localmente uma nota de devolução legítima antes mesmo de chegar à API.
        var digitosEsperados = interestadual ? new[] { '2', '6' } : new[] { '5', '1' };
        if (!digitosEsperados.Contains(cfop[0]))
            throw new DomainException(
                $"{rotulo}: CFOP '{cfop}' incoerente — operação {(interestadual ? "interestadual exige CFOP 2xxx/6xxx" : "dentro do estado exige CFOP 1xxx/5xxx")}.");
    }

    public static void ValidarJustificativaCancelamento(string? justificativa)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length < 15)
            throw new DomainException("Justificativa do cancelamento deve ter no mínimo 15 caracteres.");
    }

    public static void ValidarCorrecao(string? correcao)
    {
        if (string.IsNullOrWhiteSpace(correcao) || correcao.Trim().Length < 15)
            throw new DomainException("Texto da carta de correção deve ter no mínimo 15 caracteres.");

        var proibidas = new[] { "VALOR", "DESTINATARIO", "DESTINATÁRIO", "IMPOSTO", "PRECO", "PREÇO" };
        var textoMaiusculo = correcao.ToUpperInvariant();
        if (proibidas.Any(textoMaiusculo.Contains))
            throw new DomainException($"Carta de correção {TextoProibidoCorrecao}");
    }

    public static void ValidarInutilizacao(string? justificativa, string numeroInicial, string numeroFinal)
    {
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Trim().Length < 15)
            throw new DomainException("Justificativa da inutilização deve ter no mínimo 15 caracteres.");

        if (!int.TryParse(numeroInicial, out var inicial) || !int.TryParse(numeroFinal, out var final))
            throw new DomainException("Número inicial e final da faixa devem ser numéricos.");

        if (inicial > final)
            throw new DomainException("Número inicial da faixa não pode ser maior que o final.");
    }
}
