using ImperialColors.Application.DTOs;
using System.Globalization;
using System.Text;

namespace ImperialColors.Application.Helpers;

public static class ListaCompraWhatsAppHelper
{
    public static string ObterSaudacao(DateTime? agora = null)
    {
        var hora = (agora ?? DateTime.Now).Hour;
        if (hora < 12) return "Bom dia";
        if (hora < 18) return "Boa tarde";
        return "Olá";
    }

    public static string MontarMensagemPedido(string nomeFornecedor, IEnumerable<ItemListaCompraDto> itens, DateTime? agora = null)
    {
        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        var sb = new StringBuilder();
        sb.AppendLine($"{ObterSaudacao(agora)}, {nomeFornecedor.Trim()}! Aqui é da Imperial Colors.");
        sb.AppendLine();
        sb.AppendLine("Segue a nossa lista de pedido para cotação:");
        sb.AppendLine();

        foreach (var item in itens.OrderBy(i => i.NomeProduto, StringComparer.OrdinalIgnoreCase))
        {
            var qtd = item.QuantidadeDesejada % 1m == 0m
                ? item.QuantidadeDesejada.ToString("N0", cultura)
                : item.QuantidadeDesejada.ToString("N1", cultura);
            sb.AppendLine($"- {qtd} {item.Unidade} | {item.NomeProduto}");
        }

        sb.AppendLine();
        sb.AppendLine("Ficamos no aguardo do orçamento. Obrigado!");
        return sb.ToString().TrimEnd();
    }

    public static string SomenteDigitos(string? valor)
        => string.IsNullOrEmpty(valor) ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());

    public static string? NormalizarTelefoneWhatsApp(string? whatsApp, string? telefone)
    {
        var bruto = !string.IsNullOrWhiteSpace(whatsApp) ? whatsApp : telefone;
        if (string.IsNullOrWhiteSpace(bruto))
            return null;

        var digits = SomenteDigitos(bruto);
        if (digits.Length is < 10 or > 13)
            return null;

        return digits.Length <= 11 ? "55" + digits : digits;
    }
}
