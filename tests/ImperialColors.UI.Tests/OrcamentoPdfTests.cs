using ImperialColors.Application.Configuration;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
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
/// O PDF do orçamento é o que chega na mão do cliente: precisa trazer os dados da empresa
/// vindos das Configurações, os itens, os totais e o aviso de que não é venda nem documento
/// fiscal.
/// </summary>
public class OrcamentoPdfTests
{
    private static OrcamentoDto CriarOrcamentoExemplo() => new()
    {
        Id = 1,
        NumeroOrcamento = "ORC-20260914-0001",
        NomeCliente = "Joao da Silva",
        TelefoneCliente = "(41) 99999-1111",
        DataOrcamento = new DateTime(2026, 9, 14),
        DataValidade = new DateTime(2026, 9, 21),
        Subtotal = 500m,
        Desconto = 50m,
        Total = 450m,
        Status = StatusOrcamento.Aberto,
        Observacoes = "Entrega combinada para a semana seguinte.",
        Usuario = "Atendente Teste",
        Itens =
        [
            new ItemOrcamentoDto
            {
                ProdutoId = 10,
                NomeProduto = "Tinta Acrilica Branca",
                CodigoProduto = "TAB-001",
                Unidade = "GL",
                Quantidade = 2m,
                PrecoUnitario = 100m,
                Subtotal = 200m
            },
            new ItemOrcamentoDto
            {
                ProdutoId = null,
                NomeProduto = "Mao de obra de pintura",
                Unidade = "UN",
                Quantidade = 1m,
                PrecoUnitario = 300m,
                Subtotal = 300m
            }
        ]
    };

    [Fact]
    public async Task GerarOrcamentoPdf_DeveTrazerEmpresaClienteItensETotais()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "ImperialColorsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pastaTemp);

        await File.WriteAllTextAsync(Path.Combine(pastaTemp, "appsettings.json"),
            """
            {
              "DadosEmpresa": {
                "NomeFantasia": "Imperial Colors",
                "RazaoSocial": "Imperial Colors Comercio LTDA",
                "Subtitulo": "Tintas e Revestimentos",
                "CNPJ": "12.345.678/0001-99",
                "Endereco": "Av. Principal, 1000 - Curitiba - PR",
                "Telefone": "(41) 3333-4444"
              },
              "Cupom": {
                "MensagemRodape": "Obrigado pela preferencia!"
              }
            }
            """);

        var pdfPath = Path.Combine(pastaTemp, "orcamento.pdf");
        var relatorio = CriarRelatorioService(pastaTemp, out var provider);
        await using (provider)
        {
            await relatorio.GerarOrcamentoPdfAsync(CriarOrcamentoExemplo(), pdfPath);
        }

        Assert.True(File.Exists(pdfPath));

        var texto = ExtrairTextoPdf(pdfPath);

        Assert.Contains("ORC-20260914-0001", texto);

        // Cabecalho = papel timbrado: nome, razao social, subtitulo, documentos e contatos.
        Assert.Contains("IMPERIAL COLORS", texto);
        Assert.Contains("Imperial Colors Comercio LTDA", texto);
        Assert.Contains("Tintas e Revestimentos", texto);
        Assert.Contains("12.345.678/0001-99", texto);
        Assert.Contains("Av. Principal, 1000", texto);
        Assert.Contains("(41) 3333-4444", texto);

        // O numero do orcamento saiu do titulo e passou para o bloco de dados.
        Assert.DoesNotContain("Orcamento ORC-20260914-0001", texto);
        Assert.Contains("Orcamento: ORC-20260914-0001", texto);

        Assert.Contains("Joao da Silva", texto);
        Assert.Contains("(41) 99999-1111", texto);
        Assert.Contains("Tinta Acrilica Branca", texto);
        Assert.Contains("Mao de obra de pintura", texto);
        Assert.Contains("TAB-001", texto);
        Assert.Contains("21/09/2026", texto);
        Assert.Contains("Entrega combinada", texto);
        Assert.Contains("Obrigado pela preferencia!", texto);
    }

    [Fact]
    public async Task GerarOrcamentoPdf_DeveAvisarQueNaoEhVendaNemDocumentoFiscal()
    {
        var pastaTemp = Path.Combine(Path.GetTempPath(), "ImperialColorsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pastaTemp);

        await File.WriteAllTextAsync(Path.Combine(pastaTemp, "appsettings.json"),
            """
            { "DadosEmpresa": { "NomeFantasia": "Imperial Colors" } }
            """);

        var pdfPath = Path.Combine(pastaTemp, "orcamento-aviso.pdf");
        var relatorio = CriarRelatorioService(pastaTemp, out var provider);
        await using (provider)
        {
            await relatorio.GerarOrcamentoPdfAsync(CriarOrcamentoExemplo(), pdfPath);
        }

        var texto = ExtrairTextoPdf(pdfPath);

        Assert.Contains("sem valor fiscal", texto);
        Assert.Contains("sem compromisso de venda", texto);
        Assert.DoesNotContain("CUPOM", texto);
    }

    private static IRelatorioService CriarRelatorioService(string pastaTemp, out ServiceProvider provider)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(pastaTemp)
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
