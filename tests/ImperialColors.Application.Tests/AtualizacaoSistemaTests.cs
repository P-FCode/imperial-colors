using ImperialColors.Infrastructure.Atualizacao;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// A atualização automática tem um modo de falha silencioso que é pior que um erro na tela:
/// a tag da release não é entendida, vira 0.0.0, 0.0.0 nunca é maior que a versão instalada,
/// e o cliente recebe "você já está na versão mais recente" para sempre — sem nunca receber a
/// correção, e sem ninguém perceber que não recebeu.
///
/// É por isso que a conversão de tag é uma função isolada e testada: ela decide se a
/// atualização chega ou não ao cliente.
/// </summary>
public class VersaoReleaseTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("V1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    // Erro de digitação comum ao criar a release na mão.
    [InlineData("v.1.2.3", 1, 2, 3)]
    [InlineData("  v1.2.3  ", 1, 2, 3)]
    // Duas partes é uma tag legítima e vale 1.3.0, não 1.0.0.
    [InlineData("v1.3", 1, 3, 0)]
    [InlineData("v2", 2, 0, 0)]
    // O quarto componente do assembly não pode atrapalhar a comparação.
    [InlineData("1.2.3.4", 1, 2, 3)]
    // Pré-lançamento e metadados de build são descartados, não rejeitados.
    [InlineData("v1.4.0-beta.2", 1, 4, 0)]
    [InlineData("v1.4.0+20260901", 1, 4, 0)]
    // Formato que o build realmente grava: o SourceLink anexa o SHA do commit na
    // InformationalVersion. Conferido num build real com -p:Version=1.7.2.
    [InlineData("1.7.2+5921f14c04468082b0b784572dfe72355467e6ea", 1, 7, 2)]
    public void TagValida_EConvertida(string tag, int maior, int menor, int correcao)
    {
        Assert.True(VersaoRelease.TentarConverter(tag, out var versao));
        Assert.Equal(new Version(maior, menor, correcao), versao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("release-final")]
    [InlineData("v")]
    [InlineData("versao-nova")]
    [InlineData("1.2.3.4.5")]
    // Números negativos e não numéricos precisam ser recusados, não virar zero em silêncio.
    [InlineData("v-1.2.3")]
    [InlineData("v1.x.3")]
    public void TagInvalida_ERecusadaEmVezDeViraZero(string? tag)
    {
        Assert.False(VersaoRelease.TentarConverter(tag, out _));
    }

    /// <summary>
    /// O caso que a truncagem existe para resolver: a versão do assembly tem quatro
    /// componentes (1.2.3.0) e a tag tem três (1.2.3). Sem truncar, 1.2.3.0 &gt; 1.2.3 e o
    /// sistema nunca ofereceria a atualização da própria versão que acabou de sair.
    /// </summary>
    [Fact]
    public void VersaoDoAssembly_ComparaDeIgualParaIgualComATag()
    {
        var doAssembly = VersaoRelease.Truncar(new Version(1, 2, 3, 0));
        Assert.True(VersaoRelease.TentarConverter("v1.2.3", out var daTag));

        Assert.Equal(daTag, doAssembly);
        Assert.False(daTag > doAssembly);

        Assert.True(VersaoRelease.TentarConverter("v1.2.4", out var proxima));
        Assert.True(proxima > doAssembly);
    }

    [Fact]
    public void Truncar_TrataVersaoSemBuildComoZero()
    {
        // new Version(1, 2) deixa Build em -1; sem o Math.Max isso viraria "v1.2.-1".
        Assert.Equal(new Version(1, 2, 0), VersaoRelease.Truncar(new Version(1, 2)));
        Assert.Equal("v1.2.0", VersaoRelease.Formatar(new Version(1, 2)));
    }
}

/// <summary>
/// Escolha do pacote dentro da release. Uma release traz outros arquivos junto (código-fonte
/// zipado, que o GitHub anexa sozinho), então pegar "o primeiro anexo" instalaria o
/// código-fonte por cima da instalação do cliente.
/// </summary>
public class SelecaoPacoteReleaseTests
{
    private static AtualizadorSistemaService.ArquivoGitHub Arquivo(string nome, long tamanho = 1024) =>
        new() { Nome = nome, UrlDownload = $"https://exemplo/{nome}", Tamanho = tamanho };

    [Fact]
    public void NomeExato_TemPrioridadeSobreOsDemaisAnexos()
    {
        var escolhido = AtualizadorSistemaService.SelecionarArquivo(
        [
            Arquivo("Source code (zip)"),
            Arquivo("outro-instalador.zip"),
            Arquivo(AtualizadorSistemaService.NomeArquivoPreferido),
        ]);

        Assert.NotNull(escolhido);
        Assert.Equal(AtualizadorSistemaService.NomeArquivoPreferido, escolhido!.Nome);
    }

    [Fact]
    public void SemNomeExato_CaiNoZipDoProprioSistema()
    {
        var escolhido = AtualizadorSistemaService.SelecionarArquivo(
        [
            Arquivo("manual.pdf"),
            Arquivo("ImperialColors-win-x64-v2.zip"),
        ]);

        Assert.Equal("ImperialColors-win-x64-v2.zip", escolhido?.Nome);
    }

    [Fact]
    public void ReleaseSemAnexos_NaoEscolheNada()
    {
        Assert.Null(AtualizadorSistemaService.SelecionarArquivo(null));
        Assert.Null(AtualizadorSistemaService.SelecionarArquivo([]));
        Assert.Null(AtualizadorSistemaService.SelecionarArquivo([Arquivo("notas.pdf")]));
    }
}

/// <summary>
/// O script de troca de arquivos roda depois que o aplicativo fecha, sem janela e sem
/// ninguém olhando. Estas são as garantias que impedem que uma falha ali deixe um caixa sem
/// sistema — se alguma sumir do script, some junto a rede de proteção.
/// </summary>
public class ScriptAtualizacaoTests
{
    [Fact]
    public void Env_EstaNaListaDePreservados()
    {
        // O .env guarda as credenciais do banco. Sobrescrevê-lo apontaria o PDV para um
        // banco que não existe — o sistema atualiza e não abre mais.
        Assert.Contains(".env", ScriptAplicacaoAtualizacao.ArquivosPreservados);
    }

    [Fact]
    public void Script_NaoCopiaNadaEnquantoOAplicativoEstiverAberto()
    {
        var script = ScriptAplicacaoAtualizacao.Gerar();

        // Espera o processo sair antes de qualquer cópia, e desiste sem tocar em arquivo
        // nenhum se ele não sair: copiar por cima de um executável em uso falha no meio e
        // deixa a instalação metade nova, metade velha.
        Assert.Contains("Get-Process -Id $PidEspera", script);
        Assert.Contains("Nenhum arquivo foi alterado", script);
        Assert.Contains("exit 2", script);
    }

    [Fact]
    public void Script_FazCopiaDeSegurancaERestauraQuandoFalha()
    {
        var script = ScriptAplicacaoAtualizacao.Gerar();

        Assert.Contains("Copia de seguranca criada", script);
        Assert.Contains("Restaurando a versao anterior", script);
        Assert.Contains("Versao anterior restaurada", script);
    }

    [Fact]
    public void Script_ReabreOSistemaEmTodosOsCaminhosDeFalha()
    {
        var script = ScriptAplicacaoAtualizacao.Gerar();

        // Terminar sem reabrir deixaria o operador diante de uma tela vazia sem saber o que
        // fazer. Toda saída depois do ponto em que o app já fechou tem que chamar Reabrir.
        Assert.Contains("function Reabrir", script);
        var reaberturas = script.Split("Reabrir").Length - 1;
        Assert.True(reaberturas >= 4, $"esperava Reabrir em todos os caminhos, achei {reaberturas - 1} chamadas");
    }

    /// <summary>
    /// Trava o bug que um teste do script em pastas de mentira revelou: a copia de seguranca
    /// usava <c>Copy-Item -LiteralPath</c> com curinga, e <c>-LiteralPath</c> trata o
    /// <c>*</c> como nome literal de arquivo — copiava zero arquivos e nao levantava erro
    /// nenhum. O backup ficava vazio em silencio e a restauracao registrava "versao anterior
    /// restaurada" sem ter restaurado nada: a rede de protecao existia, dizia que tinha
    /// funcionado, e nao segurava.
    /// </summary>
    [Fact]
    public void Script_ConfereQueACopiaDeSegurancaNaoSaiuVazia()
    {
        var script = ScriptAplicacaoAtualizacao.Gerar();

        Assert.Contains("A copia de seguranca ficou vazia", script);
        // Curinga em Copy-Item exige -Path; com -LiteralPath a copia e silenciosamente vazia.
        Assert.DoesNotContain("Copy-Item -LiteralPath (Join-Path", script);
    }

    [Fact]
    public void Script_RegistraOQueFezEmArquivo()
    {
        var script = ScriptAplicacaoAtualizacao.Gerar();

        // Sem log, uma falha do auxiliar vira "o sistema não abriu mais" e não há por onde
        // começar a investigar — ele roda sem janela.
        Assert.Contains("Add-Content -LiteralPath $Log", script);
    }
}
