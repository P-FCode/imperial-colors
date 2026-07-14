using System.Windows;
using System.Windows.Threading;

namespace ImperialColors.UI.Helpers;

public static class UiDispatcher
{
    public static void ExecutarNaUi(Action acao)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
            acao();
        else
            dispatcher.Invoke(acao);
    }

    public static async Task ExecutarNaUiAsync(Func<Task> acao)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        if (dispatcher.CheckAccess())
            await acao();
        else
            await dispatcher.InvokeAsync(acao);
    }
}
