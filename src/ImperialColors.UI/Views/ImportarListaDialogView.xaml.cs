using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace ImperialColors.UI.Views;

/// <summary>
/// Escolha do formato do arquivo antes de importar a lista de conferência de uma venda
/// externa. O tutorial abaixo das opções muda junto com a seleção: o operador monta a
/// planilha ou o arquivo no formato certo olhando o exemplo, em vez de descobrir o layout
/// errado só depois, numa mensagem de erro de importação.
/// </summary>
public partial class ImportarListaDialogView : Window
{
    public FormatoImportacaoLista Formato { get; private set; } = FormatoImportacaoLista.Xlsx;

    /// <summary>Arquivo escolhido pelo operador — só preenchido quando o diálogo fecha com
    /// "Importar".</summary>
    public string? CaminhoArquivo { get; private set; }

    private string? _arquivoEmSelecao;

    public ImportarListaDialogView()
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        AtualizarTutorial();
    }

    private FormatoImportacaoLista FormatoMarcado => true switch
    {
        _ when RbCsv.IsChecked == true => FormatoImportacaoLista.Csv,
        _ when RbTxt.IsChecked == true => FormatoImportacaoLista.Txt,
        _ => FormatoImportacaoLista.Xlsx
    };

    private void Formato_Changed(object sender, RoutedEventArgs e)
    {
        // Disparado uma vez antes do InitializeComponent terminar de montar os três
        // RadioButtons (o IsChecked do XAML já levanta o evento), quando TxtComoMontar
        // ainda é null.
        if (TxtComoMontar is null) return;

        AtualizarTutorial();

        // Trocar de formato depois de escolher o arquivo deixaria um .xlsx selecionado sob
        // a instrução de CSV — a importação falharia com um erro sobre o conteúdo, não
        // sobre a escolha. Melhor pedir o arquivo de novo, já com o filtro certo.
        LimparSelecao();
    }

    private void AtualizarTutorial()
    {
        switch (FormatoMarcado)
        {
            case FormatoImportacaoLista.Csv:
                TxtComoMontar.Text =
                    "Uma linha por item, com os campos separados por ponto e vírgula (;) — é assim que o Excel em português salva ao escolher \"CSV\". " +
                    "Arquivo separado por vírgula também é aceito. A linha de cabeçalho pode ficar: se a quantidade não for um número, ela é ignorada. " +
                    "Nome que contenha o separador precisa vir entre aspas.";
                TxtExemplo.Text =
                    """
                    codigo;nome;quantidade
                    7891234567890;Tinta Branca 18L;2
                    ;Cor especial avulsa;1
                    ;"Tinta Branca, 18L";1,5
                    """;
                break;

            case FormatoImportacaoLista.Txt:
                TxtComoMontar.Text =
                    $"Uma linha por item, no formato {VendaExternaImportHelper.FormatoEsperado}. " +
                    "Linhas que começam com # são ignoradas, o que serve para anotar a lista.";
                TxtExemplo.Text =
                    """
                    # conferência da rua - 12/03
                    7891234567890;Tinta Branca 18L;2
                    ;Cor especial avulsa;1
                    """;
                break;

            default:
                TxtComoMontar.Text =
                    "A primeira aba da planilha, com três colunas: A = código de barras, B = nome do produto, C = quantidade. " +
                    "A linha de cabeçalho pode ficar — se a coluna C não trouxer um número, ela é ignorada. " +
                    "Formato .xlsx (Excel 2007 em diante); planilha antiga .xls precisa ser salva como .xlsx antes.";
                TxtExemplo.Text =
                    """
                          A                 B                      C
                    1   codigo            nome                  quantidade
                    2   7891234567890     Tinta Branca 18L      2
                    3                     Cor especial avulsa   1
                    """;
                break;
        }
    }

    private void BtnSelecionarArquivo_Click(object sender, RoutedEventArgs e)
    {
        var formato = FormatoMarcado;

        var dialogo = new OpenFileDialog
        {
            Title = "Selecionar lista de produtos",
            Filter = formato switch
            {
                FormatoImportacaoLista.Csv => "Arquivo CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
                FormatoImportacaoLista.Txt => "Arquivo de texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*",
                _ => "Planilha do Excel (*.xlsx)|*.xlsx"
            }
        };

        if (dialogo.ShowDialog() != true)
            return;

        LimparErro();
        _arquivoEmSelecao = dialogo.FileName;
        TxtArquivoSelecionado.Text = Path.GetFileName(dialogo.FileName);
        BtnImportar.IsEnabled = true;
    }

    private void BtnImportar_Click(object sender, RoutedEventArgs e)
    {
        if (_arquivoEmSelecao is null)
        {
            MostrarErro("Selecione o arquivo da lista antes de importar.");
            return;
        }

        if (!File.Exists(_arquivoEmSelecao))
        {
            MostrarErro("O arquivo selecionado não está mais disponível. Selecione novamente.");
            LimparSelecao();
            return;
        }

        Formato = FormatoMarcado;
        CaminhoArquivo = _arquivoEmSelecao;
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);

    private void LimparSelecao()
    {
        _arquivoEmSelecao = null;
        TxtArquivoSelecionado.Text = "Nenhum arquivo selecionado";
        BtnImportar.IsEnabled = false;
    }

    private void MostrarErro(string mensagem)
    {
        TxtErro.Text = mensagem;
        TxtErro.Visibility = Visibility.Visible;
    }

    private void LimparErro()
    {
        TxtErro.Text = string.Empty;
        TxtErro.Visibility = Visibility.Collapsed;
    }
}
