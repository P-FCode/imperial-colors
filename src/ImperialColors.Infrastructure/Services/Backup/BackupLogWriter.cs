using System.Text;

namespace ImperialColors.Infrastructure.Services.Backup;

public static class BackupLogWriter
{
    public static void Registrar(string diretorioRaiz, string mensagem, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(diretorioRaiz);
            var caminhoLog = Path.Combine(diretorioRaiz, "backup_erros.log");
            var linha = new StringBuilder()
                .Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ")
                .Append(mensagem);

            if (ex is not null)
                linha.Append(" | ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);

            File.AppendAllText(caminhoLog, linha.AppendLine().ToString());
        }
        catch
        {
            // Falha silenciosa — backup não pode interromper o PDV.
        }
    }
}
