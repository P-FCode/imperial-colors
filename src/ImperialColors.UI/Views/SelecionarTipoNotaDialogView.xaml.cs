using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Windows;

namespace ImperialColors.UI.Views;

/// <summary>
/// Passo intermediário entre "Emitir Nota" na tela de Vendas e a tela de emissão: NF-e e
/// NFC-e não são intercambiáveis (modelo 55 exige destinatário completo, modelo 65 é venda
/// de balcão), e a escolha muda o que a SEFAZ vai cobrar do rascunho montado a partir da
/// venda — por isso é uma decisão explícita do operador, não um padrão silencioso.
/// </summary>
public partial class SelecionarTipoNotaDialogView : Window
{
    public TipoNotaFiscal? TipoSelecionado { get; private set; }

    public SelecionarTipoNotaDialogView(VendaDto venda, IReadOnlyList<NotaFiscalResumoDto>? notasDaVenda = null)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);

        TxtTitulo.Text = $"Emitir nota fiscal da venda #{venda.NumeroVenda}";
        TxtResumoVenda.Text =
            $"{venda.NomeCompradorExibicao}  •  {venda.Itens.Count} " +
            (venda.Itens.Count == 1 ? "item" : "itens") +
            $"  •  Total {FormattingHelper.FormatarMoeda(venda.Total)}";

        MostrarNotasExistentes(notasDaVenda);
    }

    /// <summary>Nota anterior da mesma venda que não virou documento fiscal (rascunho salvo
    /// ou rejeitada pela SEFAZ). Não bloqueia — quem bloqueia é o Service, e só para nota
    /// viva —, mas avisa antes de o operador criar um segundo rascunho sem perceber que já
    /// existe um esperando na lista de notas.</summary>
    private void MostrarNotasExistentes(IReadOnlyList<NotaFiscalResumoDto>? notas)
    {
        if (notas is null || notas.Count == 0) return;

        var descricoes = notas.Select(n =>
            $"{(n.Tipo == TipoNotaFiscal.NFCe ? "NFC-e" : "NF-e")} {n.Serie}/{n.Numero} ({NotaFiscalStatusHelper.Descricao(n.Status)})");

        TxtAviso.Text =
            "Esta venda já tem nota em aberto: " + string.Join(", ", descricoes) + ". " +
            "Se continuar, um novo rascunho será criado — o anterior continua na lista de notas e pode ser excluído por lá.";
        PainelAviso.Visibility = Visibility.Visible;
    }

    private void BtnNFe_Click(object sender, RoutedEventArgs e) => Confirmar(TipoNotaFiscal.NFe);

    private void BtnNFCe_Click(object sender, RoutedEventArgs e) => Confirmar(TipoNotaFiscal.NFCe);

    private void Confirmar(TipoNotaFiscal tipo)
    {
        TipoSelecionado = tipo;
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);
}
