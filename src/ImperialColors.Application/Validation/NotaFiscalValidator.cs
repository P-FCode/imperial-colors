using System.Text.RegularExpressions;
using ImperialColors.Application.DTOs;
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

            if (validarNcm && string.IsNullOrWhiteSpace(item.Ncm))
                throw new DomainException($"{rotulo}: NCM é obrigatório (Configurações → Fiscal → Validar NCM em notas está ligado).");

            ValidarCfop(item.Cfop, interestadual, rotulo);
            ValidarIcmsItem(item, rotulo);

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

    private static void ValidarIcmsItem(ItemNotaFiscalDto item, string rotulo)
    {
        var temCst = !string.IsNullOrWhiteSpace(item.CstIcms);
        var temCsosn = !string.IsNullOrWhiteSpace(item.CsosnIcms);

        if (temCst && temCsosn)
            throw new DomainException($"{rotulo}: preencha apenas CST ou apenas CSOSN do ICMS, nunca os dois.");

        if (!temCst && !temCsosn)
            throw new DomainException($"{rotulo}: falta CST ou CSOSN do ICMS.");
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
