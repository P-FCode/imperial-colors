using System.Diagnostics;

namespace ImperialColors.UI.Helpers;

public static class WhatsAppHelper
{
    public static void AbrirChat(string telefoneInternacional, string mensagem)
    {
        var url = $"https://wa.me/{telefoneInternacional}?text={Uri.EscapeDataString(mensagem)}";
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
