using System.Diagnostics;
using System.IO;

namespace ImperialColors.UI.Helpers;

/// <summary>
/// Imprime o PDF do DANFE (baixado da API Fiscal) direto na impressora térmica
/// configurada em Configurações → Periféricos. Diferente de <see cref="CupomPrintHelper"/>
/// (que imprime um <see cref="System.Windows.FrameworkElement"/> via
/// <c>PrintDialog.PrintVisual</c>), aqui a entrada é um PDF pronto — o sistema não tem
/// biblioteca de renderização de PDF, então a rota mais simples e confiável no Windows é
/// gravar em um arquivo temporário e disparar o verbo "printto" do leitor de PDF padrão
/// do sistema, apontando para a impressora configurada.
/// </summary>
public static class DanfePrintHelper
{
    public static bool ImprimirNaImpressoraConfigurada(
        byte[] pdfDanfe,
        string nomeArquivo,
        string? nomeImpressora,
        out string? mensagemErro)
    {
        mensagemErro = null;

        if (string.IsNullOrWhiteSpace(nomeImpressora))
        {
            mensagemErro = "Nenhuma impressora configurada. Acesse Configurações → Periféricos.";
            return false;
        }

        try
        {
            var caminhoTemp = Path.Combine(Path.GetTempPath(), $"{nomeArquivo}.pdf");
            File.WriteAllBytes(caminhoTemp, pdfDanfe);

            var processo = Process.Start(new ProcessStartInfo
            {
                FileName = caminhoTemp,
                Verb = "printto",
                Arguments = $"\"{nomeImpressora}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            processo?.WaitForInputIdle(5000);
            return true;
        }
        catch (Exception ex)
        {
            mensagemErro = $"Erro ao imprimir o DANFE: {ex.Message}. Verifique se há um leitor de PDF padrão instalado (ex.: Adobe Reader, Edge).";
            return false;
        }
    }
}
