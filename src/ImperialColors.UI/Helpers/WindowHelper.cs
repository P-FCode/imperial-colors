using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ImperialColors.UI.Helpers;

public static class WindowHelper
{
    public static async Task ExibirCupomAsync(IServiceProvider serviceProvider, VendaDto venda, Window? owner = null)
    {
        var vendaService = serviceProvider.GetRequiredService<IVendaService>();
        var vendaCompleta = await vendaService.ObterComItensAsync(venda.Id);

        if (vendaCompleta is null)
        {
            MessageBox.Show(
                $"Venda #{venda.NumeroVenda} não encontrada.",
                "Cupom",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var cupom = serviceProvider.GetRequiredService<CupomView>();
        cupom.InicializarVenda(vendaCompleta);
        ModalWindowHelper.ExibirDialogo(cupom, owner);
    }
}
