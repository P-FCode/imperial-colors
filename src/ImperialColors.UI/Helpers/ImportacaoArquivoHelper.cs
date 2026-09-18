using ClosedXML.Excel;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Exceptions;
using System.Globalization;
using System.IO;
using System.Text;

namespace ImperialColors.UI.Helpers;

/// <summary>
/// Leitura física dos arquivos de importação de lista (venda externa). Fica na camada de
/// apresentação porque é aqui que o ClosedXML vive — junto da geração de relatórios em
/// Excel. O que sai daqui é sempre texto cru por célula; quem valida formato, cabeçalho,
/// quantidade e número da linha é o <c>VendaExternaImportHelper</c>, um lugar só para os
/// três formatos.
/// </summary>
public static class ImportacaoArquivoHelper
{
    /// <summary>Primeira aba da planilha, três colunas: A = código de barras, B = nome,
    /// C = quantidade.</summary>
    public static IReadOnlyList<LinhaBrutaImportacaoDto> LerXlsx(string caminhoArquivo)
    {
        using var planilha = Abrir(caminhoArquivo);

        var aba = planilha.Worksheets.FirstOrDefault()
            ?? throw new DomainException("A planilha não tem nenhuma aba.");

        var linhas = aba.RowsUsed()
            .Select(linha => new LinhaBrutaImportacaoDto
            {
                NumeroLinha = linha.RowNumber(),
                CodigoBarras = LerCelula(linha.Cell(1)),
                NomeProduto = LerCelula(linha.Cell(2)),
                Quantidade = LerCelula(linha.Cell(3))
            })
            .ToList();

        if (linhas.Count == 0)
            throw new DomainException($"A aba '{aba.Name}' da planilha está vazia.");

        return linhas;
    }

    /// <summary>
    /// Conteúdo de um TXT/CSV como texto.
    ///
    /// A codificação é detectada em vez de assumida: "Salvar como CSV" no Excel em português
    /// grava em ANSI (Windows-1252), não em UTF-8 — lido como UTF-8, "Solução Limpadora"
    /// chega ao cadastro como "Solu��o Limpadora" e o item entra na venda com o nome
    /// corrompido. Tenta UTF-8 estrito primeiro (que é o que um arquivo moderno ou exportado
    /// por outro sistema usa) e só cai para Latin-1 quando os bytes não formam UTF-8 válido.
    /// </summary>
    public static async Task<string> LerTextoAsync(string caminhoArquivo, CancellationToken cancellationToken = default)
    {
        var bytes = await File.ReadAllBytesAsync(caminhoArquivo, cancellationToken);

        // BOM não é conteúdo: deixado no texto, ele vira o primeiro caractere do código de
        // barras da primeira linha e o produto nunca é encontrado no estoque.
        var inicio = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;

        try
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(bytes, inicio, bytes.Length - inicio);
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes, inicio, bytes.Length - inicio);
        }
    }

    /// <summary>
    /// O erro cru do ClosedXML para um arquivo que não é .xlsx (um .xls antigo renomeado,
    /// por exemplo) fala de ZIP e de partes do pacote OpenXML — não diz ao operador o que
    /// fazer. Traduzir aqui é o que transforma isso numa instrução acionável.
    /// </summary>
    private static XLWorkbook Abrir(string caminhoArquivo)
    {
        try
        {
            return new XLWorkbook(caminhoArquivo);
        }
        catch (Exception ex) when (ex is not DomainException)
        {
            throw new DomainException(
                "Não foi possível abrir a planilha. Confirme que o arquivo é .xlsx (Excel 2007 em diante) " +
                "e que não está corrompido — uma planilha .xls antiga precisa ser salva como .xlsx antes.");
        }
    }

    /// <summary>
    /// Célula como o operador a vê. Número precisa de tratamento próprio: um código de
    /// barras digitado numa célula sem formatação de texto é guardado como número pelo
    /// Excel, e converter isso pela cultura da máquina devolveria algo como
    /// "7,89123456789E+12" ou "7891234567890,00" — nenhum dos dois casa com o código
    /// cadastrado no estoque.
    /// </summary>
    private static string LerCelula(IXLCell celula)
    {
        var valor = celula.Value;

        return valor.IsNumber
            ? ((decimal)valor.GetNumber()).ToString("0.##########", CultureInfo.InvariantCulture)
            : celula.GetString().Trim();
    }
}
