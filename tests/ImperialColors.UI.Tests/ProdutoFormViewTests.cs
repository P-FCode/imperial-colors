using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Views;
using Moq;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace ImperialColors.UI.Tests;

public class FormattingHelperTests
{
    public FormattingHelperTests()
    {
        FormattingHelper.ConfigurarCulturaThread();
    }

    [Fact]
    public void FormatarMoeda_DeveUsarRealBrasileiro()
    {
        Assert.Equal("R$ 45,50", FormattingHelper.FormatarMoeda(45.50m));
        Assert.Equal("R$ 89,90", FormattingHelper.FormatarMoeda(89.90m));
    }

    [Fact]
    public void TryParseMoeda_DeveInterpretarFormatoBrasileiro()
    {
        Assert.True(FormattingHelper.TryParseMoeda("R$ 45,50", out var valor));
        Assert.Equal(45.50m, valor);
    }

    [Theory]
    [InlineData(10, "UN", "10 Unidades")]
    [InlineData(1, "UN", "1 Unidade")]
    [InlineData(3.6, "LT", "3,6 Litros")]
    [InlineData(2, "CX", "2 Caixas")]
    public void FormatarQuantidadeUnidade_DeveRetornarTextoAmigavel(decimal qtd, string unidade, string esperado)
    {
        Assert.Equal(esperado, FormattingHelper.FormatarQuantidadeUnidade(qtd, unidade));
    }

    [Fact]
    public void FormatarDataHora_DeveUsarPadraoBrasileiro()
    {
        var data = new DateTime(2026, 6, 10, 14, 30, 0);
        Assert.Equal("10/06/2026 14:30", FormattingHelper.FormatarDataHora(data));
    }
}

public class ProdutoFormViewTests
{
    public ProdutoFormViewTests()
    {
        WpfTestBootstrap.Inicializar();
    }

    [StaFact]
    public void ProdutoFormView_AbrirEFecharCincoVezesSemExcecao()
    {
        for (var i = 0; i < 5; i++)
        {
            var excecao = Record.Exception(() =>
            {
                var form = CriarForm();
                form.InicializarNovo();
                form.InicializarEdicao(CriarProdutoExemplo());
                form.Close();
            });

            Assert.Null(excecao);
        }
    }

    [StaFact]
    public void ProdutoFormView_ComboBoxesNaoUsamDisplayMemberPathComItemTemplate()
    {
        var form = CriarForm();

        form.InicializarNovo();
        form.Show();

        try
        {
            var categoria = form.FindName("CmbCategoria") as ComboBox;
            var marca = form.FindName("CmbMarca") as ComboBox;

            Assert.NotNull(categoria);
            Assert.NotNull(marca);
            Assert.True(string.IsNullOrEmpty(categoria!.DisplayMemberPath));
            Assert.True(string.IsNullOrEmpty(marca!.DisplayMemberPath));
            Assert.NotNull(categoria.ItemTemplate);
            Assert.NotNull(marca.ItemTemplate);
        }
        finally
        {
            form.Close();
        }
    }

    [StaFact]
    public void ProdutoFormView_EdicaoExibeMoedaFormatada()
    {
        var form = CriarForm();

        form.InicializarEdicao(CriarProdutoExemplo());

        Assert.Equal("R$ 45,50", (form.FindName("TxtCusto") as TextBox)?.Text);
        Assert.Equal("R$ 89,90", (form.FindName("TxtPrecoVenda") as TextBox)?.Text);
        form.Close();
    }

    [StaFact]
    public void ProdutoFormView_ComboBoxesNaoContemPlaceholderIdZero()
    {
        var form = CriarForm();

        form.InicializarNovo();
        form.Show();

        try
        {
            var categoria = form.FindName("CmbCategoria") as ComboBox;
            var marca = form.FindName("CmbMarca") as ComboBox;

            Assert.All(categoria!.Items.Cast<CategoriaDto>(), c => Assert.True(c.Id > 0));
            Assert.All(marca!.Items.Cast<MarcaDto>(), m => Assert.True(m.Id > 0));
        }
        finally
        {
            form.Close();
        }
    }

    /// <summary>O antigo <c>CmbLitragemGl</c> (dropdown fixo 3,6L/18L, só para Galão) foi
    /// substituído por um campo de texto livre, para qualquer unidade — confirma que o
    /// campo novo existe, aceita edição/limpeza, e que o dropdown antigo não sobrou.</summary>
    [StaFact]
    public void ProdutoFormView_TamanhoEmbalagemEhTextoLivrePreenchidoNaEdicaoELimpoNoNovo()
    {
        var form = CriarForm();

        Assert.Null(form.FindName("CmbLitragemGl"));
        Assert.Null(form.FindName("PainelLitragemGl"));

        var produto = CriarProdutoExemplo();
        produto.Unidade = "BA";
        produto.TamanhoEmbalagem = "25 KG";
        form.InicializarEdicao(produto);

        var campo = form.FindName("TxtTamanhoEmbalagem") as TextBox;
        Assert.NotNull(campo);
        Assert.Equal("25 KG", campo!.Text);

        form.InicializarNovo();
        Assert.Equal(string.Empty, campo.Text);

        form.Close();
    }

    /// <summary>O peso é guardado em gramas, mas conferido em quilos — o eco ao lado do
    /// campo é o que impede um zero a mais (55000) de passar batido. Cobre também a volta
    /// ao formulário em branco, para o peso do produto anterior não ficar na tela.</summary>
    [StaFact]
    public void ProdutoFormView_PesoEmGramasEcoaOEquivalenteEmQuilos()
    {
        var form = CriarForm();

        var produto = CriarProdutoExemplo();
        produto.PesoGramas = 5500;
        form.InicializarEdicao(produto);

        var campo = form.FindName("TxtPesoGramas") as TextBox;
        var eco = form.FindName("TxtPesoEquivalente") as TextBlock;
        Assert.NotNull(campo);
        Assert.NotNull(eco);
        Assert.Equal("5500", campo!.Text);
        Assert.Equal("= 5,5 kg", eco!.Text);
        Assert.Equal(Visibility.Visible, eco.Visibility);

        campo.Text = "800";
        Assert.Equal("= 800 g", eco.Text);

        // Texto que não é peso não ecoa nada — quem barra de fato é a validação ao salvar.
        campo.Text = "abc";
        Assert.Equal(Visibility.Collapsed, eco.Visibility);

        form.InicializarNovo();
        Assert.Equal(string.Empty, campo.Text);
        Assert.Equal(Visibility.Collapsed, eco.Visibility);

        form.Close();
    }

    private static Mock<IProdutoService> CriarProdutoServiceMock()
    {
        var mock = new Mock<IProdutoService>();
        mock.Setup(s => s.GerarProximoCodigoInternoAsync()).ReturnsAsync("P00001");
        mock.Setup(s => s.GerarCodigoInternoPorNomeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("MCC001");
        mock.Setup(s => s.CodigoBarrasExisteAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return mock;
    }

    private static Mock<ICategoriaService> CriarCategoriaServiceMock()
    {
        var mock = new Mock<ICategoriaService>();
        mock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(new List<CategoriaDto>
        {
            new() { Id = 1, Nome = "Tintas Acrílicas" }
        });
        mock.Setup(s => s.CriarAsync(It.IsAny<string>())).ReturnsAsync(new CategoriaDto { Id = 2, Nome = "Nova" });
        return mock;
    }

    private static Mock<IMarcaService> CriarMarcaServiceMock()
    {
        var mock = new Mock<IMarcaService>();
        mock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(new List<MarcaDto>
        {
            new() { Id = 1, Nome = "Suvinil" }
        });
        mock.Setup(s => s.CriarAsync(It.IsAny<string>())).ReturnsAsync(new MarcaDto { Id = 2, Nome = "Nova" });
        return mock;
    }

    private static Mock<IFornecedorService> CriarFornecedorServiceMock()
    {
        var mock = new Mock<IFornecedorService>();
        mock.Setup(s => s.ObterTodosAsync()).ReturnsAsync(new List<FornecedorDto>());
        return mock;
    }

    private static ProdutoFormView CriarForm()
        => new(
            CriarProdutoServiceMock().Object,
            CriarCategoriaServiceMock().Object,
            CriarMarcaServiceMock().Object,
            CriarFornecedorServiceMock().Object,
            Mock.Of<IConfiguracaoFiscalService>(),
            Mock.Of<INcmService>());

    private static ProdutoDto CriarProdutoExemplo() => new()
    {
        Id = 1,
        CodigoInterno = "P00001",
        Nome = "Tinta Branca",
        Custo = 45.50m,
        PrecoVenda = 89.90m,
        QuantidadeEstoque = 10,
        EstoqueMinimo = 2,
        Unidade = "LT",
        CategoriaId = 1,
        MarcaId = 1
    };
}
