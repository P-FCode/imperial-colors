using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Helpers;

public static class PagamentoHelper
{
    public static string ObterDescricao(FormaPagamento forma, int parcelas = 1)
        => forma switch
        {
            FormaPagamento.Dinheiro => "Dinheiro",
            FormaPagamento.CartaoDebito => "Cartão de Débito",
            FormaPagamento.CartaoCredito when parcelas > 1 => $"Cartão de Crédito - {parcelas}x",
            FormaPagamento.CartaoCredito => "Cartão de Crédito",
            FormaPagamento.Pix => "Pix",
            FormaPagamento.Boleto => "Boleto",
            _ => forma.ToString()
        };

    public static string ObterDescricaoComposta(
        IReadOnlyList<VendaPagamentoDto> pagamentos,
        FormaPagamento formaLegada,
        int parcelasLegadas)
    {
        if (pagamentos.Count == 0)
            return ObterDescricao(formaLegada, parcelasLegadas);

        if (pagamentos.Count == 1)
            return pagamentos[0].Descricao;

        return "Pagamento Misto";
    }

    public static bool PermiteParcelamento(FormaPagamento forma)
        => forma == FormaPagamento.CartaoCredito;

    public static bool UsaTroco(FormaPagamento forma)
        => forma == FormaPagamento.Dinheiro;

    public static bool ValorPagoAutomatico(FormaPagamento forma)
        => forma != FormaPagamento.Dinheiro;

    public static (decimal ValorPago, decimal Troco, int Parcelas) CalcularPagamento(
        FormaPagamento forma,
        decimal total,
        decimal valorRecebido,
        int parcelas)
    {
        return forma switch
        {
            FormaPagamento.Dinheiro => (valorRecebido, Math.Max(0, valorRecebido - total), 1),
            FormaPagamento.CartaoCredito => (total, 0, Math.Clamp(parcelas, 1, 12)),
            _ => (total, 0, 1)
        };
    }

    public static void ValidarPagamento(FormaPagamento forma, decimal total, decimal valorRecebido, int parcelas)
    {
        if (total <= 0)
            throw new DomainException("Total da venda deve ser maior que zero.");

        if (forma == FormaPagamento.Dinheiro && valorRecebido < total)
            throw new DomainException("Valor recebido insuficiente para cobrir o total da venda.");

        if (forma == FormaPagamento.CartaoCredito && (parcelas < 1 || parcelas > 12))
            throw new DomainException("Selecione parcelas entre 1x e 12x.");
    }

    public static List<CriarVendaPagamentoDto> NormalizarPagamentos(CriarVendaDto dto, decimal totalVenda)
    {
        if (dto.Pagamentos.Count > 0)
            return dto.Pagamentos;

        ValidarPagamento(dto.FormaPagamento, totalVenda, dto.ValorPago, dto.QuantidadeParcelas);
        var (valorPago, _, parcelas) = CalcularPagamento(
            dto.FormaPagamento, totalVenda, dto.ValorPago, dto.QuantidadeParcelas);

        return
        [
            new CriarVendaPagamentoDto
            {
                FormaPagamento = dto.FormaPagamento,
                Valor = totalVenda,
                ValorRecebido = UsaTroco(dto.FormaPagamento) ? valorPago : null,
                QuantidadeParcelas = parcelas
            }
        ];
    }

    public static void ValidarPagamentosCompostos(decimal totalVenda, IReadOnlyList<CriarVendaPagamentoDto> pagamentos)
    {
        if (totalVenda <= 0)
            throw new DomainException("Total da venda deve ser maior que zero.");

        if (pagamentos is null || pagamentos.Count == 0)
            throw new DomainException("Informe ao menos uma forma de pagamento.");

        decimal somaAlocada = 0;

        foreach (var pagamento in pagamentos)
        {
            if (pagamento.Valor <= 0)
                throw new DomainException("Cada pagamento deve ter valor maior que zero.");

            if (PermiteParcelamento(pagamento.FormaPagamento))
            {
                if (pagamento.QuantidadeParcelas < 1 || pagamento.QuantidadeParcelas > 12)
                    throw new DomainException("Selecione parcelas entre 1x e 12x para cartão de crédito.");
            }
            else if (pagamento.QuantidadeParcelas != 1)
            {
                pagamento.QuantidadeParcelas = 1;
            }

            if (UsaTroco(pagamento.FormaPagamento))
            {
                var recebido = pagamento.ValorRecebido ?? pagamento.Valor;
                if (recebido < pagamento.Valor)
                    throw new DomainException("Valor recebido em dinheiro insuficiente para o montante informado.");
            }
            else if (pagamento.ValorRecebido.HasValue && pagamento.ValorRecebido != pagamento.Valor)
            {
                throw new DomainException("Valor recebido em espécie só se aplica a pagamentos em dinheiro.");
            }

            somaAlocada += pagamento.Valor;
        }

        if (somaAlocada != totalVenda)
            throw new DomainException(
                $"A soma dos pagamentos ({somaAlocada:C}) deve ser igual ao total da venda ({totalVenda:C}).");
    }

    public static (FormaPagamento FormaResumo, int ParcelasResumo, decimal ValorPagoResumo, decimal TrocoTotal)
        ResumirPagamentosLegado(IReadOnlyList<CriarVendaPagamentoDto> pagamentos)
    {
        var formaResumo = pagamentos[0].FormaPagamento;
        var parcelasResumo = pagamentos.Count == 1 ? pagamentos[0].QuantidadeParcelas : 1;
        var valorPagoResumo = pagamentos.Sum(p => p.Valor);
        var trocoTotal = pagamentos
            .Where(p => UsaTroco(p.FormaPagamento))
            .Sum(p => Math.Max(0, (p.ValorRecebido ?? p.Valor) - p.Valor));

        return (formaResumo, parcelasResumo, valorPagoResumo, trocoTotal);
    }
}
