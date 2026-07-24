using System.Windows;
using ImperialColors.Application.DTOs;

namespace ImperialColors.UI.Views;

public partial class LogAuditoriaDetalheView : Window
{
    public LogAuditoriaDetalheView(LogAuditoriaDto log)
    {
        InitializeComponent();
        TxtDataHora.Text = log.DataHora.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        TxtNivel.Text = log.NivelDescricao;
        TxtModuloAcao.Text = $"{log.Modulo} / {log.Acao}";
        TxtUsuario.Text = log.NomeUsuario;
        TxtDescricao.Text = log.Descricao;
        TxtPayload.Text = string.IsNullOrWhiteSpace(log.PayloadJson)
            ? "(sem payload técnico)"
            : log.PayloadJson;
    }

    private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();
}
