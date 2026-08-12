using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class CalculoFiscalVendaService : ICalculoFiscalVendaService
{
    private readonly IVendaRepository _vendaRepository;
    private readonly ITributacaoProdutoRepository _tributacaoRepository;
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;

    public CalculoFiscalVendaService(
        IVendaRepository vendaRepository,
        ITributacaoProdutoRepository tributacaoRepository,
        IConfiguracaoFiscalService configuracaoFiscal)
    {
        _vendaRepository = vendaRepository;
        _tributacaoRepository = tributacaoRepository;
        _configuracaoFiscal = configuracaoFiscal;
    }

    public async Task<TotaisFiscaisVendaDto> CalcularAsync(int vendaId, CancellationToken cancellationToken = default)
    {
        var venda = await _vendaRepository.ObterComItensAsync(vendaId)
            ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var aliquotaIbsUf = empresa.AliquotaIbsUfPadrao ?? 0m;
        var aliquotaIbsMunicipio = empresa.AliquotaIbsMunicipioPadrao ?? 0m;
        var aliquotaCbs = empresa.AliquotaCbsPadrao ?? 0m;

        var totais = new TotaisFiscaisVendaDto
        {
            VendaId = venda.Id,
            NumeroVenda = venda.NumeroVenda,
            VProd = venda.Subtotal,
            VDesc = venda.Desconto,
            VNf = venda.Total
        };

        foreach (var item in venda.Itens)
        {
            var nomeProduto = item.Produto?.Nome ?? $"Produto Id {item.ProdutoId}";
            var tributacao = await _tributacaoRepository.ObterPorProdutoIdAsync(item.ProdutoId, cancellationToken);

            if (tributacao is null)
            {
                totais.Avisos.Add($"'{nomeProduto}': produto sem tributação cadastrada — nenhum imposto calculado para este item.");
                totais.Itens.Add(new ItemCalculoFiscalDto
                {
                    ProdutoId = item.ProdutoId,
                    NomeProduto = nomeProduto,
                    ValorItem = item.Subtotal
                });
                continue;
            }

            var resultadoItem = CalculoFiscalHelper.CalcularItem(
                item.ProdutoId,
                nomeProduto,
                item.Subtotal,
                tributacao.CstIcms,
                tributacao.CsosnIcms,
                tributacao.AliquotaIcms,
                tributacao.CstPis,
                tributacao.AliquotaPis,
                tributacao.CstCofins,
                tributacao.AliquotaCofins,
                tributacao.CstIpi,
                tributacao.AliquotaIpi,
                aliquotaIbsUf,
                aliquotaIbsMunicipio,
                aliquotaCbs);

            totais.Itens.Add(resultadoItem);
            totais.Avisos.AddRange(resultadoItem.Avisos);

            // Base do ICMS/PIS/COFINS só soma quando o item efetivamente destacou valor
            // (mantém vBC coerente com vICMS/vPIS/vCOFINS, igual o MathematicalValidator
            // da API de emissão exige).
            if (resultadoItem.VIcms > 0) totais.VBcIcms += item.Subtotal;
            if (resultadoItem.VPis > 0) totais.VBcPis += item.Subtotal;
            if (resultadoItem.VCofins > 0) totais.VBcCofins += item.Subtotal;

            totais.VIcms += resultadoItem.VIcms;
            totais.VPis += resultadoItem.VPis;
            totais.VCofins += resultadoItem.VCofins;
            totais.VBcIbsCbs += item.Subtotal;
            totais.VIbsUf += resultadoItem.VIbsUf;
            totais.VIbsMunicipio += resultadoItem.VIbsMunicipio;
            totais.VCbs += resultadoItem.VCbs;
        }

        totais.VIbs = totais.VIbsUf + totais.VIbsMunicipio;
        return totais;
    }
}
