using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Atualizacao;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// A trava de coordenação entre PDVs vive inteira nesta decisão: comparar a versão desta
/// instalação com o que está gravado no parâmetro compartilhado, e decidir se avisa e se
/// regrava. Testado com o repositório mockado — não precisa de banco real, a lógica que
/// importa é a comparação de versões e QUANDO regravar, não a persistência em si (essa é a
/// responsabilidade, já testada em outro lugar, de <c>ParametroSistemaRepository</c>).
/// </summary>
public class CoordenacaoAtualizacaoBancoServiceTests
{
    private static Mock<IParametroSistemaRepository> CriarRepositorioComValor(string? valorAtual)
    {
        var repositorio = new Mock<IParametroSistemaRepository>();
        repositorio
            .Setup(r => r.ObterTextoAsync(ParametroSistemaChaves.VersaoBancoAplicada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(valorAtual);
        return repositorio;
    }

    private static CoordenacaoAtualizacaoBancoService CriarServico(Mock<IParametroSistemaRepository> repositorio) =>
        new(repositorio.Object, NullLogger<CoordenacaoAtualizacaoBancoService>.Instance);

    /// <summary>Extraído à parte porque <c>It.Is&lt;T&gt;</c> monta uma árvore de expressão,
    /// que não aceita <c>out var</c> nem descarte inline.</summary>
    private static bool RegistraExatamenteAVersao(string valorGravado, Version esperada) =>
        ImperialColors.Domain.Helpers.RegistroVersaoBancoHelper.TentarConverter(valorGravado, out var lida, out _)
        && lida == esperada;

    /// <summary>
    /// Banco novo, ninguém abriu ainda: não pode travar o primeiro caixa a abrir, e tem que
    /// gravar a própria versão como ponto de partida para os próximos.
    /// </summary>
    [Fact]
    public async Task SemRegistroAnterior_NaoAvisaEGravaAPropriaVersaoComoBase()
    {
        var repositorio = CriarRepositorioComValor(null);
        var servico = CriarServico(repositorio);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.False(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        Assert.Null(resultado.VersaoRegistradaNoBanco);

        repositorio.Verify(r => r.SalvarTextoAsync(
            ParametroSistemaChaves.VersaoBancoAplicada,
            It.Is<string>(v => RegistraExatamenteAVersao(v, resultado.VersaoInstalada)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// O caso que a trava existe para pegar: outro caixa, com versão mais nova, já abriu o
    /// banco. Este caixa tem que ser avisado E não pode rebaixar o registro — se rebaixasse,
    /// o próximo caixa atualizado abriria e não veria mais o aviso de que ainda existe um
    /// caixa desatualizado por aí.
    /// </summary>
    [Fact]
    public async Task RegistroMaisNovoQueEstaInstalacao_AvisaENaoRegrava()
    {
        var registroFuturo = ImperialColors.Domain.Helpers.RegistroVersaoBancoHelper.Formatar(
            new Version(99, 0, 0), "DESKTOP-CAIXA2");
        var repositorio = CriarRepositorioComValor(registroFuturo);
        var servico = CriarServico(repositorio);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.True(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        Assert.Equal(new Version(99, 0, 0), resultado.VersaoRegistradaNoBanco);
        Assert.Equal("DESKTOP-CAIXA2", resultado.MaquinaQueAtualizouPorUltimo);

        repositorio.Verify(r => r.SalvarTextoAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// O caixa que acabou de atualizar: sua versão é maior que a registrada (que ainda é a
    /// antiga, de antes dele reiniciar). Não pode ser avisado sobre si mesmo, e TEM que
    /// regravar — é assim que os outros caixas, ainda antigos, ficam sabendo depois.
    /// </summary>
    [Fact]
    public async Task EstaInstalacaoEMaisNovaQueORegistro_NaoAvisaEAvancaORegistro()
    {
        var registroAntigo = ImperialColors.Domain.Helpers.RegistroVersaoBancoHelper.Formatar(
            new Version(0, 0, 1), "DESKTOP-CAIXA1");
        var repositorio = CriarRepositorioComValor(registroAntigo);
        var servico = CriarServico(repositorio);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.False(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        repositorio.Verify(r => r.SalvarTextoAsync(
            ParametroSistemaChaves.VersaoBancoAplicada, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Versão idêntica à registrada: nem aviso, nem regravação. O registro não é um log de
    /// todo acesso — regravar a cada abertura apagaria a informação de qual caixa foi
    /// realmente o primeiro a avançar aquela versão, sem ganhar nada em troca.
    /// </summary>
    [Fact]
    public async Task VersaoIgualARegistrada_NaoAvisaENaoRegrava()
    {
        var meuAssembly = ImperialColors.Infrastructure.Atualizacao.AtualizadorSistemaService.ObterVersaoInstalada();
        var registroIgual = ImperialColors.Domain.Helpers.RegistroVersaoBancoHelper.Formatar(
            meuAssembly, "DESKTOP-CAIXA1");
        var repositorio = CriarRepositorioComValor(registroIgual);
        var servico = CriarServico(repositorio);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.False(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        repositorio.Verify(r => r.SalvarTextoAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Um parâmetro corrompido (edição manual, formato de uma versão futura do sistema) não
    /// pode travar a abertura do sistema. Trata como se não houvesse registro: não avisa, e
    /// estabelece um novo baseline — a ausência de coordenação nunca pode ser pior do que a
    /// coordenação que ela substitui.
    /// </summary>
    [Fact]
    public async Task ValorCorrompido_TrataComoSemRegistroEmVezDeFalhar()
    {
        var repositorio = CriarRepositorioComValor("isto-nao-e-um-registro-valido");
        var servico = CriarServico(repositorio);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.False(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        Assert.Null(resultado.VersaoRegistradaNoBanco);
        Assert.Null(resultado.MaquinaQueAtualizouPorUltimo);

        repositorio.Verify(r => r.SalvarTextoAsync(
            ParametroSistemaChaves.VersaoBancoAplicada, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
