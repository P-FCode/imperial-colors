using System.IO;
using System.Text;
using ClosedXML.Excel;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Leitura física dos arquivos de importação de lista. Os dois pontos cobertos aqui são os
/// que quebram silenciosamente na mão do operador: código de barras que o Excel guardou
/// como número e CSV salvo em ANSI pelo próprio Excel em português.
/// </summary>
public class ImportacaoArquivoHelperTests : IDisposable
{
    private readonly List<string> _arquivosTemporarios = [];

    public void Dispose()
    {
        foreach (var arquivo in _arquivosTemporarios)
        {
            try { File.Delete(arquivo); } catch (IOException) { /* arquivo temporário */ }
        }

        GC.SuppressFinalize(this);
    }

    private string CriarCaminhoTemporario(string extensao)
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"imperial_import_{Guid.NewGuid():N}{extensao}");
        _arquivosTemporarios.Add(caminho);
        return caminho;
    }

    private string CriarPlanilha(Action<IXLWorksheet> preencher)
    {
        var caminho = CriarCaminhoTemporario(".xlsx");
        using var planilha = new XLWorkbook();
        preencher(planilha.AddWorksheet("Lista"));
        planilha.SaveAs(caminho);
        return caminho;
    }

    /// <summary>
    /// Regressão do código de barras numérico: digitado numa célula sem formatação de
    /// texto, o Excel guarda 7891234567890 como número. Convertido pela cultura da máquina
    /// viraria "7,89123456789E+12" ou "7891234567890,00" — nenhum dos dois casa com o
    /// código cadastrado no estoque, e o item entraria como manual sem ninguém entender
    /// por quê.
    /// </summary>
    [Fact]
    public void LerXlsx_CodigoDeBarrasGuardadoComoNumero_VoltaComoDigitos()
    {
        var caminho = CriarPlanilha(aba =>
        {
            aba.Cell(1, 1).Value = 7891234567890d;
            aba.Cell(1, 2).Value = "Tinta Branca 18L";
            aba.Cell(1, 3).Value = 2;
        });

        var linha = Assert.Single(ImportacaoArquivoHelper.LerXlsx(caminho));

        Assert.Equal("7891234567890", linha.CodigoBarras);
        Assert.Equal("Tinta Branca 18L", linha.NomeProduto);
        Assert.Equal("2", linha.Quantidade);
    }

    [Fact]
    public void LerXlsx_ComCabecalhoEItens_DevolveONumeroRealDaLinhaDaPlanilha()
    {
        var caminho = CriarPlanilha(aba =>
        {
            aba.Cell(1, 1).Value = "codigo";
            aba.Cell(1, 2).Value = "nome";
            aba.Cell(1, 3).Value = "quantidade";
            aba.Cell(2, 2).Value = "Cor especial avulsa";
            aba.Cell(2, 3).Value = 1.5;
        });

        var linhas = ImportacaoArquivoHelper.LerXlsx(caminho);

        Assert.Equal(2, linhas.Count);
        Assert.Equal(1, linhas[0].NumeroLinha);
        Assert.Equal(2, linhas[1].NumeroLinha);
        Assert.Equal(string.Empty, linhas[1].CodigoBarras);
        Assert.Equal("1.5", linhas[1].Quantidade);
    }

    [Fact]
    public void LerXlsx_PlanilhaSemNenhumaCelulaPreenchida_LancaDomainException()
    {
        var caminho = CriarPlanilha(_ => { });

        Assert.Throws<DomainException>(() => ImportacaoArquivoHelper.LerXlsx(caminho));
    }

    /// <summary>Um .xls antigo renomeado para .xlsx é o engano mais provável do operador —
    /// o erro cru do ClosedXML fala de ZIP e partes OpenXML, o que não diz o que fazer.</summary>
    [Fact]
    public async Task LerXlsx_ArquivoQueNaoEhPlanilha_LancaDomainExceptionComInstrucao()
    {
        var caminho = CriarCaminhoTemporario(".xlsx");
        await File.WriteAllTextAsync(caminho, "7891234567890;Tinta Branca 18L;2");

        var ex = Assert.Throws<DomainException>(() => ImportacaoArquivoHelper.LerXlsx(caminho));

        Assert.Contains(".xlsx", ex.Message);
    }

    /// <summary>Regressão do BOM: deixado no texto, ele vira o primeiro caractere do código
    /// de barras da primeira linha e o produto nunca é encontrado no estoque.</summary>
    [Fact]
    public async Task LerTextoAsync_ArquivoUtf8ComBom_NaoDeixaOBomNoConteudo()
    {
        var caminho = CriarCaminhoTemporario(".csv");
        await File.WriteAllTextAsync(caminho, "7891234567890;Solução Limpadora;2", new UTF8Encoding(true));

        var conteudo = await ImportacaoArquivoHelper.LerTextoAsync(caminho);

        Assert.StartsWith("7891234567890", conteudo, StringComparison.Ordinal);
        Assert.Contains("Solução Limpadora", conteudo);
    }

    /// <summary>"Salvar como CSV" no Excel em português grava em ANSI, não em UTF-8 — lido
    /// como UTF-8, o nome chegaria ao cadastro com os acentos corrompidos.</summary>
    [Fact]
    public async Task LerTextoAsync_ArquivoEmAnsi_PreservaOsAcentos()
    {
        var caminho = CriarCaminhoTemporario(".csv");
        await File.WriteAllBytesAsync(caminho, Encoding.Latin1.GetBytes("7891234567890;Solução Limpadora;2"));

        var conteudo = await ImportacaoArquivoHelper.LerTextoAsync(caminho);

        Assert.Contains("Solução Limpadora", conteudo);
    }
}
