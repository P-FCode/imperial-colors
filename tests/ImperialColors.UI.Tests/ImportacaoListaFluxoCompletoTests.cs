using System.IO;
using System.Text;
using ClosedXML.Excel;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Services;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Interfaces;
using ImperialColors.UI.Helpers;
using Moq;
using Xunit;

namespace ImperialColors.UI.Tests;

/// <summary>
/// Caminho completo da importação de lista, do arquivo em disco até os itens prontos para a
/// grade de conferência — exatamente a sequência que <c>VendaExternaFormView</c> executa
/// depois que o operador escolhe o formato e o arquivo.
///
/// Os testes de unidade cobrem cada peça isolada (leitura do arquivo, interpretação das
/// linhas); o que se verifica aqui é que as peças se encaixam e que os três formatos
/// produzem o MESMO resultado para a mesma lista — se um deles divergir, o operador vê a
/// conferência mudar só por ter salvo a planilha de outro jeito.
/// </summary>
public class ImportacaoListaFluxoCompletoTests : IDisposable
{
    private const string CodigoCadastrado = "7891234567890";
    private const decimal PrecoDeTabela = 189.90m;

    private readonly List<string> _arquivosTemporarios = [];

    public void Dispose()
    {
        foreach (var arquivo in _arquivosTemporarios)
        {
            try { File.Delete(arquivo); } catch (IOException) { /* arquivo temporário */ }
        }

        GC.SuppressFinalize(this);
    }

    private string CriarCaminho(string extensao)
    {
        var caminho = Path.Combine(Path.GetTempPath(), $"imperial_fluxo_{Guid.NewGuid():N}{extensao}");
        _arquivosTemporarios.Add(caminho);
        return caminho;
    }

    /// <summary>Serviço real, com só o estoque mockado: um produto cadastrado no código de
    /// barras usado nos três arquivos.</summary>
    private static VendaExternaService CriarServico()
    {
        var produtoRepositorio = new Mock<IProdutoRepository>();
        produtoRepositorio.Setup(r => r.ObterPorCodigoBarrasAsync(CodigoCadastrado)).ReturnsAsync(new Produto
        {
            Id = 42,
            CodigoInterno = "P001",
            CodigoBarras = CodigoCadastrado,
            Nome = "Tinta Acrílica Branca 18L",
            Unidade = "UN",
            PrecoVenda = PrecoDeTabela
        });

        return new VendaExternaService(
            Mock.Of<IVendaExternaRepository>(),
            produtoRepositorio.Object,
            Mock.Of<IAuditoriaService>(),
            Mock.Of<IUsuarioAtual>());
    }

    /// <summary>Mesma lista nos três formatos: cabeçalho, um item com código cadastrado e um
    /// item sem código (avulso da rua).</summary>
    private string CriarXlsx()
    {
        var caminho = CriarCaminho(".xlsx");
        using var planilha = new XLWorkbook();
        var aba = planilha.AddWorksheet("Lista");

        aba.Cell(1, 1).Value = "codigo";
        aba.Cell(1, 2).Value = "nome";
        aba.Cell(1, 3).Value = "quantidade";
        // Código digitado sem formatar a coluna como texto — o Excel guarda como número.
        aba.Cell(2, 1).Value = 7891234567890d;
        aba.Cell(2, 2).Value = "Tinta que o operador digitou errado";
        aba.Cell(2, 3).Value = 2;
        aba.Cell(3, 2).Value = "Cor especial avulsa";
        aba.Cell(3, 3).Value = 1;

        planilha.SaveAs(caminho);
        return caminho;
    }

    private async Task<string> CriarCsvAsync()
    {
        var caminho = CriarCaminho(".csv");
        // ANSI e com cabeçalho: é o que sai de "Salvar como CSV" no Excel em português.
        await File.WriteAllBytesAsync(caminho, Encoding.Latin1.GetBytes(
            $"""
             codigo;nome;quantidade
             {CodigoCadastrado};Tinta que o operador digitou errado;2
             ;Cor especial avulsa;1
             """));
        return caminho;
    }

    private async Task<string> CriarTxtAsync()
    {
        var caminho = CriarCaminho(".txt");
        await File.WriteAllTextAsync(caminho,
            $"""
             # conferência da rua
             {CodigoCadastrado};Tinta que o operador digitou errado;2
             ;Cor especial avulsa;1
             """);
        return caminho;
    }

    /// <summary>A sequência que a tela executa: leitura do arquivo conforme o formato e, daí
    /// para frente, o mesmo serviço para os três.</summary>
    private static async Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ImportarComoATelaFaz(
        VendaExternaService servico, string caminho, FormatoImportacaoLista formato)
        => formato == FormatoImportacaoLista.Xlsx
            ? await servico.ProcessarImportacaoPlanilhaAsync(ImportacaoArquivoHelper.LerXlsx(caminho))
            : await servico.ProcessarImportacaoTextoAsync(
                await ImportacaoArquivoHelper.LerTextoAsync(caminho), formato);

    private async Task<string> CriarArquivo(FormatoImportacaoLista formato) => formato switch
    {
        FormatoImportacaoLista.Xlsx => CriarXlsx(),
        FormatoImportacaoLista.Csv => await CriarCsvAsync(),
        _ => await CriarTxtAsync()
    };

    [Theory]
    [InlineData(FormatoImportacaoLista.Xlsx)]
    [InlineData(FormatoImportacaoLista.Csv)]
    [InlineData(FormatoImportacaoLista.Txt)]
    public async Task ImportarLista_NosTresFormatos_ProduzOsMesmosItensDeConferencia(FormatoImportacaoLista formato)
    {
        var servico = CriarServico();
        var caminho = await CriarArquivo(formato);

        var linhas = await ImportarComoATelaFaz(servico, caminho, formato);

        Assert.Equal(2, linhas.Count); // o cabeçalho/comentário não vira item

        // Item com código cadastrado: nome e preço vêm do estoque, não do arquivo — é isso
        // que faz o item entrar na venda como "Estoque" (com baixa).
        var doEstoque = linhas[0];
        Assert.Equal(CodigoCadastrado, doEstoque.CodigoBarras);
        Assert.Equal(42, doEstoque.ProdutoId);
        Assert.Equal("Tinta Acrílica Branca 18L", doEstoque.NomeProduto);
        Assert.Equal(PrecoDeTabela, doEstoque.PrecoUnitario);
        Assert.Equal(2m, doEstoque.Quantidade);
        Assert.True(doEstoque.VinculadoEstoque);

        // Item sem código: mantém o nome do arquivo e fica sem preço, para o operador
        // digitar na grade.
        var avulso = linhas[1];
        Assert.Null(avulso.CodigoBarras);
        Assert.Null(avulso.ProdutoId);
        Assert.Equal("Cor especial avulsa", avulso.NomeProduto);
        Assert.Equal(1m, avulso.Quantidade);
        Assert.False(avulso.VinculadoEstoque);
    }
}
