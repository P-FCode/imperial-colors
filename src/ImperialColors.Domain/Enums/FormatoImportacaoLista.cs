namespace ImperialColors.Domain.Enums;

/// <summary>
/// Formato do arquivo usado para importar a lista de conferência de uma venda externa. As
/// colunas são sempre as mesmas (código de barras, nome do produto e quantidade) — o que
/// muda é só como elas chegam, e é isso que o operador escolhe antes de selecionar o
/// arquivo.
/// </summary>
public enum FormatoImportacaoLista
{
    /// <summary>Planilha do Excel — três colunas na primeira aba.</summary>
    Xlsx = 0,

    /// <summary>Texto separado por ponto e vírgula (ou vírgula), com aspas opcionais.</summary>
    Csv = 1,

    /// <summary>Texto separado por ponto e vírgula, sem aspas — formato original do módulo.</summary>
    Txt = 2
}
