using ClosedXML.Excel;
using ImperialColors.Application.Configuration;
using ImperialColors.Application.DTOs;
using ImperialColors.UI.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Coluna de peso no relatório de estoque. As duas exportações tratam o mesmo dado de
/// formas diferentes de propósito — o PDF é para conferir na mão ("5,5 kg"), a planilha é
/// para somar e filtrar (número em quilos) —, e é essa diferença que os testes travam.
/// </summary>
public class RelatorioEstoquePesoTests : IDisposable
{
    private readonly string _pastaTemp = Path.Combine(
        Path.GetTempPath(), "ImperialColorsTests", Guid.NewGuid().ToString("N"));

    public RelatorioEstoquePesoTests()
    {
        Directory.CreateDirectory(_pastaTemp);
        File.WriteAllText(Path.Combine(_pastaTemp, "appsettings.json"),
            """
            { "DadosEmpresa": { "NomeFantasia": "Imperial Colors" } }
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_pastaTemp, recursive: true); } catch (IOException) { /* pasta temporária */ }
        GC.SuppressFinalize(this);
    }

    private static IReadOnlyList<ProdutoDto> Produtos() =>
    [
        new()
        {
            CodigoInterno = "TAB-001",
            Nome = "Tinta Acrilica Branca",
            CategoriaNome = "Tintas",
            MarcaNome = "Coral",
            QuantidadeEstoque = 10,
            Unidade = "GL",
            PesoGramas = 5500,
            PrecoVenda = 189.90m
        },
        new()
        {
            CodigoInterno = "PIN-002",
            Nome = "Pincel 2 polegadas",
            CategoriaNome = "Acessorios",
            MarcaNome = "Atlas",
            QuantidadeEstoque = 30,
            Unidade = "UN",
            PesoGramas = null, // item sem peso cadastrado — o normal no catálogo existente
            PrecoVenda = 19.90m
        }
    ];

    private IRelatorioService CriarRelatorioService(out ServiceProvider provider)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_pastaTemp)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions<EmpresaConfig>().Bind(configuration.GetSection(EmpresaConfig.Secao));
        services.PostConfigure<EmpresaConfig>(EmpresaConfigEnvironmentOverrides.Aplicar);
        services.AddSingleton<IAppConfigService, AppConfigService>();
        services.AddSingleton<IRelatorioService, RelatorioService>();

        provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IRelatorioService>();
    }

    [Fact]
    public async Task Pdf_TrazAColunaDePesoLegivelEUmTracoQuandoNaoCadastrado()
    {
        var caminho = Path.Combine(_pastaTemp, "estoque.pdf");
        var relatorio = CriarRelatorioService(out var provider);
        await using (provider)
        {
            await relatorio.GerarRelatorioEstoquePdfAsync(Produtos(), caminho);
        }

        var texto = ExtrairTextoPdf(caminho);

        Assert.Contains("Peso", texto);
        Assert.Contains("5,5 kg", texto);
        // As outras colunas continuam no lugar — a coluna nova não empurrou nenhuma para fora
        // da largura da página.
        Assert.Contains("TAB-001", texto);
        Assert.Contains("Tinta Acrilica Branca", texto);
        Assert.Contains("Pincel 2 polegadas", texto);
        Assert.Contains("R$ 189,90", texto);
    }

    /// <summary>
    /// Na planilha o peso sai como NÚMERO em quilos, não como o texto "5,5 kg" do PDF: a
    /// coluna existe ali para ser somada (peso total da carga) e filtrada (acima de X kg).
    /// Texto quebraria as duas coisas em silêncio.
    /// </summary>
    [Fact]
    public async Task Excel_TrazOPesoComoNumeroEmQuilosSomavel()
    {
        var caminho = Path.Combine(_pastaTemp, "estoque.xlsx");
        var relatorio = CriarRelatorioService(out var provider);
        await using (provider)
        {
            await relatorio.GerarRelatorioEstoqueExcelAsync(Produtos(), caminho);
        }

        using var planilha = new XLWorkbook(caminho);
        var aba = planilha.Worksheet(1);

        // Cabeçalho na linha 3; peso entre Unidade e Preco Venda.
        Assert.Equal("Peso (kg)", aba.Cell(3, 7).GetString());
        Assert.Equal("Preco Venda", aba.Cell(3, 8).GetString());

        var celulaPeso = aba.Cell(4, 7);
        Assert.True(celulaPeso.Value.IsNumber);
        Assert.Equal(5.5, celulaPeso.Value.GetNumber(), 3);

        // Produto sem peso deixa a célula vazia — zero seria um peso declarado, e entraria
        // numa soma como se o item não pesasse nada.
        Assert.True(aba.Cell(5, 7).Value.IsBlank);

        // O preço não ficou para trás ao abrir espaço para a coluna nova.
        Assert.Equal(189.90, aba.Cell(4, 8).Value.GetNumber(), 2);
    }

    private static string ExtrairTextoPdf(string caminho)
    {
        using var reader = new PdfReader(caminho);
        using var pdf = new PdfDocument(reader);
        var texto = new System.Text.StringBuilder();

        for (var i = 1; i <= pdf.GetNumberOfPages(); i++)
            texto.Append(PdfTextExtractor.GetTextFromPage(pdf.GetPage(i), new LocationTextExtractionStrategy()));

        return texto.ToString();
    }
}
