using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Leitura da lista de conferência da venda externa nos três formatos. O arquivo é montado
/// pelo operador fora do sistema (planilha da rua, exportação de outro programa), então o
/// que se cobra aqui é tolerância ao que chega de verdade — cabeçalho junto, separador
/// diferente, nome com vírgula — sem deixar passar linha realmente quebrada em silêncio.
/// </summary>
public class VendaExternaImportHelperTests
{
    // ===== TXT — formato original do módulo =====

    [Fact]
    public void ParseTexto_Txt_InterpretaFormatoCodigoBarrasNomeQuantidade()
    {
        const string conteudo = """
            7891234567890;Tinta Branca 18L;2
            ;Cor especial avulsa;1
            """;

        var linhas = VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Txt);

        Assert.Equal(2, linhas.Count);
        Assert.Equal("7891234567890", linhas[0].CodigoBarras);
        Assert.Equal("Tinta Branca 18L", linhas[0].NomeProduto);
        Assert.Equal(2m, linhas[0].Quantidade);
        Assert.Null(linhas[1].CodigoBarras);
        Assert.Equal("Cor especial avulsa", linhas[1].NomeProduto);
    }

    [Fact]
    public void ParseTexto_Txt_LinhaIncompleta_LancaDomainException()
    {
        const string conteudo = "123;Produto";

        var ex = Assert.Throws<DomainException>(
            () => VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Txt));

        Assert.Contains("formato inválido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseTexto_ArquivoVazio_LancaDomainException()
        => Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseTexto("   ", FormatoImportacaoLista.Txt));

    [Fact]
    public void ParseTexto_Txt_IgnoraComentarioEContaLinhaDoArquivo()
    {
        const string conteudo = """
            # conferência da rua - 12/03
            7891234567890;Tinta Branca 18L;2
            """;

        var linha = Assert.Single(VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Txt));

        // Número da linha é o do ARQUIVO (a 2ª), não o índice do item — é ele que aparece na
        // mensagem de erro e é por ele que o operador acha a linha na planilha.
        Assert.Equal(2, linha.NumeroLinha);
    }

    // ===== CSV =====

    [Fact]
    public void ParseTexto_Csv_PontoEVirgulaComCabecalho_IgnoraOCabecalho()
    {
        const string conteudo = """
            codigo;nome;quantidade
            7891234567890;Tinta Branca 18L;2
            """;

        var linha = Assert.Single(VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Csv));

        Assert.Equal("Tinta Branca 18L", linha.NomeProduto);
        Assert.Equal(2m, linha.Quantidade);
    }

    /// <summary>Arquivo vindo de outro sistema (ou do Google Planilhas) sai com vírgula — o
    /// separador é detectado pela primeira linha, não fixado.</summary>
    [Fact]
    public void ParseTexto_Csv_SeparadoPorVirgula_EhAceito()
    {
        const string conteudo = "7891234567890,Tinta Branca 18L,2";

        var linha = Assert.Single(VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Csv));

        Assert.Equal("7891234567890", linha.CodigoBarras);
        Assert.Equal("Tinta Branca 18L", linha.NomeProduto);
    }

    /// <summary>Nome com o próprio separador dentro é exatamente o que o Excel exporta entre
    /// aspas — sem tratar isso, o item entraria com o nome cortado na vírgula.</summary>
    [Fact]
    public void ParseTexto_Csv_NomeEntreAspasComOSeparadorDentro_NaoQuebraOCampo()
    {
        const string conteudo = """
            7891234567890,"Tinta Branca, 18L",2
            """;

        var linha = Assert.Single(VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Csv));

        Assert.Equal("Tinta Branca, 18L", linha.NomeProduto);
        Assert.Equal(2m, linha.Quantidade);
    }

    [Fact]
    public void ParseTexto_Csv_QuantidadeComVirgulaDecimal_EhAceita()
    {
        const string conteudo = "7891234567890;Massa corrida;1,5";

        var linha = Assert.Single(VendaExternaImportHelper.ParseTexto(conteudo, FormatoImportacaoLista.Csv));

        Assert.Equal(1.5m, linha.Quantidade);
    }

    // ===== Planilha (células já lidas pela tela) =====

    private static LinhaBrutaImportacaoDto Celulas(int numeroLinha, string? codigo, string? nome, string? quantidade)
        => new() { NumeroLinha = numeroLinha, CodigoBarras = codigo, NomeProduto = nome, Quantidade = quantidade };

    [Fact]
    public void ParseCelulas_ComCabecalho_IgnoraOCabecalhoELeOsItens()
    {
        var linhas = VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "codigo", "nome", "quantidade"),
            Celulas(2, "7891234567890", "Tinta Branca 18L", "2"),
            Celulas(3, null, "Cor especial avulsa", "1")
        ]);

        Assert.Equal(2, linhas.Count);
        Assert.Equal("7891234567890", linhas[0].CodigoBarras);
        Assert.Equal(2, linhas[0].NumeroLinha);
        Assert.Null(linhas[1].CodigoBarras);
    }

    [Fact]
    public void ParseCelulas_SemCabecalho_LeAPrimeiraLinhaComoItem()
    {
        var linhas = VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", "2")
        ]);

        Assert.Single(linhas);
        Assert.Equal("Tinta Branca 18L", linhas[0].NomeProduto);
    }

    [Fact]
    public void ParseCelulas_LinhaTotalmenteVaziaNoMeio_EhIgnorada()
    {
        var linhas = VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", "2"),
            Celulas(2, "", "", ""),
            Celulas(3, null, "Cor especial avulsa", "1")
        ]);

        Assert.Equal(2, linhas.Count);
    }

    /// <summary>
    /// O perdão de cabeçalho vale só para a PRIMEIRA linha. Texto no lugar da quantidade
    /// lá no meio da planilha é erro de digitação de verdade — se também fosse ignorado, o
    /// item sumiria da conferência sem ninguém perceber que faltou.
    /// </summary>
    [Fact]
    public void ParseCelulas_QuantidadeNaoNumericaForaDaPrimeiraLinha_LancaDomainExceptionApontandoALinha()
    {
        var ex = Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", "2"),
            Celulas(2, null, "Cor especial avulsa", "uma caixa")
        ]));

        Assert.Contains("Linha 2", ex.Message);
        Assert.Contains("quantidade inválida", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Quantidade VAZIA na primeira linha não é cabeçalho, é linha truncada — tratá-la como
    /// cabeçalho faria o primeiro item do arquivo desaparecer em silêncio.
    /// </summary>
    [Fact]
    public void ParseCelulas_PrimeiraLinhaSemQuantidade_NaoEhConfundidaComCabecalho()
    {
        var ex = Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", ""),
            Celulas(2, null, "Cor especial avulsa", "1")
        ]));

        Assert.Contains("Linha 1", ex.Message);
    }

    [Fact]
    public void ParseCelulas_SemNomeDoProduto_LancaDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", "2"),
            Celulas(2, "7891234567891", "   ", "1")
        ]));

        Assert.Contains("nome do produto", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseCelulas_SoCabecalho_LancaDomainExceptionDeArquivoSemItens()
    {
        var ex = Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "codigo", "nome", "quantidade")
        ]));

        Assert.Contains("Nenhum item válido", ex.Message);
    }

    [Fact]
    public void ParseCelulas_QuantidadeZeroOuNegativa_LancaDomainException()
    {
        Assert.Throws<DomainException>(() => VendaExternaImportHelper.ParseCelulas(
        [
            Celulas(1, "7891234567890", "Tinta Branca 18L", "0")
        ]));
    }
}
