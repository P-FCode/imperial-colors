using System.Diagnostics;
using System.IO;

namespace ImperialColors.UI.Helpers;

public static class ArquivoAnexoHelper
{
    public static async Task AbrirNoVisualizadorPadraoAsync(byte[] conteudo, string nomeArquivo)
    {
        var nomeSeguro = Path.GetFileName(nomeArquivo);
        if (string.IsNullOrWhiteSpace(nomeSeguro))
            nomeSeguro = "anexo.bin";

        var caminhoTemp = Path.Combine(Path.GetTempPath(), $"ImperialColors_{Guid.NewGuid():N}_{nomeSeguro}");
        await File.WriteAllBytesAsync(caminhoTemp, conteudo);

        Process.Start(new ProcessStartInfo
        {
            FileName = caminhoTemp,
            UseShellExecute = true
        });
    }
}
