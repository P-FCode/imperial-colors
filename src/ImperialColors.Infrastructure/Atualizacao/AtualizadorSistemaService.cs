using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Atualizacao;

/// <summary>
/// Busca a última release publicada em <c>github.com/P-FCode/imperial-colors</c>, baixa o
/// pacote anexado e prepara a troca de arquivos.
///
/// A versão instalada vem do próprio assembly, gravada pelo workflow de release a partir da
/// tag. Não existe número de versão escrito à mão em lugar nenhum do código, de propósito:
/// duas fontes de verdade para a versão significam, mais cedo ou mais tarde, um cliente na
/// versão 1.4 que se enxerga como 1.2 e reinstala a mesma coisa para sempre.
/// </summary>
public sealed class AtualizadorSistemaService : IAtualizadorSistemaService
{
    public const string Proprietario = "P-FCode";
    public const string Repositorio = "imperial-colors";

    /// <summary>Nome do pacote gerado por <c>.github/workflows/release.yml</c>.</summary>
    public const string NomeArquivoPreferido = "ImperialColors-win-x64.zip";

    private const string NomeExecutavel = "ImperialColors.exe";

    private readonly HttpClient _http;
    private readonly ILogger<AtualizadorSistemaService> _logger;

    public AtualizadorSistemaService(HttpClient http, ILogger<AtualizadorSistemaService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string VersaoInstaladaTexto => VersaoRelease.Formatar(ObterVersaoInstalada());

    /// <summary>
    /// Lê a versão de <c>AssemblyInformationalVersion</c> antes de <c>AssemblyVersion</c>: a
    /// informacional é a que o workflow preenche com a tag exata, enquanto a de assembly é
    /// obrigada pelo Windows a ter quatro números e acabaria comparando 1.2.3.0 com 1.2.3.
    /// </summary>
    internal static Version ObterVersaoInstalada()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var informacional = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (VersaoRelease.TentarConverter(informacional, out var daInformacional))
            return daInformacional;

        return VersaoRelease.Truncar(assembly.GetName().Version ?? new Version(0, 0, 0));
    }

    public async Task<ResultadoVerificacaoAtualizacaoDto> VerificarAsync(CancellationToken cancellationToken = default)
    {
        ReleaseGitHub? release;
        try
        {
            using var resposta = await _http.GetAsync(
                $"repos/{Proprietario}/{Repositorio}/releases/latest", cancellationToken);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
                return ResultadoVerificacaoAtualizacaoDto.Falha(
                    "Nenhuma versão foi publicada ainda no repositório do sistema.");

            // 403 com limite de requisições estourado é o caso comum de uso sem token: a
            // mensagem crua "403 Forbidden" faria o operador achar que perdeu o acesso.
            if (resposta.StatusCode == HttpStatusCode.Forbidden)
                return ResultadoVerificacaoAtualizacaoDto.Falha(
                    "O GitHub recusou a consulta por excesso de tentativas. Aguarde alguns minutos e tente de novo.");

            resposta.EnsureSuccessStatusCode();
            release = await resposta.Content.ReadFromJsonAsync<ReleaseGitHub>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao consultar releases do GitHub.");
            return ResultadoVerificacaoAtualizacaoDto.Falha(
                "Não foi possível falar com o GitHub. Verifique a conexão com a internet e tente novamente.");
        }

        if (release is null)
            return ResultadoVerificacaoAtualizacaoDto.Falha(
                "O GitHub devolveu uma resposta que não foi possível interpretar.");

        var instalada = ObterVersaoInstalada();

        if (!VersaoRelease.TentarConverter(release.Tag, out var publicada))
        {
            // Falhar explicitamente em vez de virar 0.0.0 e responder "você já está
            // atualizado": esse é o modo de falha em que o cliente nunca recebe a correção e
            // ninguém percebe que não recebeu.
            return ResultadoVerificacaoAtualizacaoDto.Falha(
                $"A última release está com a tag '{release.Tag}', que não é um número de versão (exemplo válido: v1.3.0). " +
                "Corrija a tag da release para a atualização automática voltar a funcionar.");
        }

        var arquivo = SelecionarArquivo(release.Arquivos);
        var temVersaoNova = publicada > instalada;

        return new ResultadoVerificacaoAtualizacaoDto
        {
            Sucesso = true,
            AtualizacaoDisponivel = temVersaoNova && arquivo is not null,
            VersaoInstalada = instalada,
            VersaoPublicada = publicada,
            NomeRelease = string.IsNullOrWhiteSpace(release.Nome) ? release.Tag : release.Nome!,
            NotasRelease = release.Corpo ?? string.Empty,
            UrlRelease = release.UrlPagina ?? string.Empty,
            UrlDownload = arquivo?.UrlDownload,
            NomeArquivo = arquivo?.Nome,
            TamanhoBytes = arquivo?.Tamanho ?? 0,
            MensagemErro = temVersaoNova && arquivo is null
                ? $"A versão {VersaoRelease.Formatar(publicada)} foi publicada, mas a release ainda não tem o pacote {NomeArquivoPreferido} anexado. " +
                  "O build automático provavelmente ainda está rodando — tente de novo em alguns minutos."
                : null
        };
    }

    /// <summary>
    /// Escolhe o pacote da release. O nome exato tem prioridade; o resto é tolerância a uma
    /// release montada à mão, em que o zip pode ter saído com outro nome.
    /// </summary>
    internal static ArquivoGitHub? SelecionarArquivo(IReadOnlyList<ArquivoGitHub>? arquivos)
    {
        if (arquivos is null || arquivos.Count == 0)
            return null;

        return arquivos.FirstOrDefault(a => a.Nome.Equals(NomeArquivoPreferido, StringComparison.OrdinalIgnoreCase))
            ?? arquivos.FirstOrDefault(a => a.Nome.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                                         && a.Nome.Contains("ImperialColors", StringComparison.OrdinalIgnoreCase))
            ?? arquivos.FirstOrDefault(a => a.Nome.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    public async Task BaixarEPrepararAsync(
        ResultadoVerificacaoAtualizacaoDto verificacao,
        IProgress<ProgressoAtualizacaoDto>? progresso = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(verificacao);

        if (!verificacao.AtualizacaoDisponivel || string.IsNullOrWhiteSpace(verificacao.UrlDownload))
            throw new InvalidOperationException("Não há atualização disponível para baixar.");

        var pastaInstalacao = AppContext.BaseDirectory
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Conferido ANTES de baixar: descobrir que falta permissão depois de 150 MB baixados,
        // com o aplicativo já fechando, é a pior hora possível para descobrir.
        GarantirPermissaoDeEscrita(pastaInstalacao);

        var raizTemporaria = Path.Combine(
            Path.GetTempPath(), "ImperialColors_Atualizacao", Guid.NewGuid().ToString("N"));
        var pastaExtracao = Path.Combine(raizTemporaria, "pacote");
        var caminhoZip = Path.Combine(raizTemporaria, verificacao.NomeArquivo ?? NomeArquivoPreferido);
        Directory.CreateDirectory(pastaExtracao);

        await BaixarAsync(verificacao, caminhoZip, progresso, cancellationToken);

        progresso?.Report(new ProgressoAtualizacaoDto { Etapa = "Extraindo arquivos..." });
        ZipFile.ExtractToDirectory(caminhoZip, pastaExtracao, overwriteFiles: true);

        var pastaOrigem = ResolverRaizDoPacote(pastaExtracao);

        progresso?.Report(new ProgressoAtualizacaoDto { Etapa = "Preparando a instalação..." });

        var caminhoScript = Path.Combine(raizTemporaria, "AplicarAtualizacao.ps1");
        var caminhoLog = Path.Combine(raizTemporaria, "atualizacao.log");
        var pastaBackup = Path.Combine(raizTemporaria, "backup");
        File.WriteAllText(caminhoScript, ScriptAplicacaoAtualizacao.Gerar());

        var argumentos =
            $"-NoProfile -ExecutionPolicy Bypass -File \"{caminhoScript}\" " +
            $"-Origem \"{pastaOrigem}\" -Destino \"{pastaInstalacao}\" " +
            $"-Executavel \"{Path.Combine(pastaInstalacao, NomeExecutavel)}\" " +
            $"-Backup \"{pastaBackup}\" -Log \"{caminhoLog}\" -PidEspera {Environment.ProcessId}";

        // Sem janela: o auxiliar não tem nada a mostrar, e um console piscando no caixa
        // parece defeito. O rastro fica no log, cujo caminho a tela informa ao operador.
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = argumentos,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = raizTemporaria
        });

        _logger.LogInformation(
            "Atualização {Versao} preparada. Log do processo auxiliar: {Log}",
            verificacao.VersaoPublicadaTexto, caminhoLog);

        CaminhoUltimoLog = caminhoLog;
    }

    /// <summary>Caminho do log do processo auxiliar, para a tela citar se algo der errado.</summary>
    public string? CaminhoUltimoLog { get; private set; }

    private async Task BaixarAsync(
        ResultadoVerificacaoAtualizacaoDto verificacao,
        string caminhoZip,
        IProgress<ProgressoAtualizacaoDto>? progresso,
        CancellationToken cancellationToken)
    {
        progresso?.Report(new ProgressoAtualizacaoDto { Etapa = "Baixando atualização...", Percentual = 0 });

        using var resposta = await _http.GetAsync(
            verificacao.UrlDownload, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var total = resposta.Content.Headers.ContentLength ?? verificacao.TamanhoBytes;
        var buffer = new byte[81920];
        long recebido = 0;

        await using (var origem = await resposta.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destino = new FileStream(caminhoZip, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            int lidos;
            while ((lidos = await origem.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destino.WriteAsync(buffer.AsMemory(0, lidos), cancellationToken);
                recebido += lidos;

                if (total > 0)
                {
                    progresso?.Report(new ProgressoAtualizacaoDto
                    {
                        Etapa = $"Baixando... {recebido / 1024d / 1024d:N1} MB de {total / 1024d / 1024d:N1} MB",
                        Percentual = recebido * 100d / total
                    });
                }
            }
        }

        // Um download truncado (queda de rede no meio) gera um zip que só falha lá na frente,
        // ao extrair, com uma mensagem sobre formato de arquivo que não ajuda ninguém. O
        // tamanho esperado vem nos metadados da release, então dá para dizer o que houve.
        if (total > 0 && recebido != total)
        {
            throw new IOException(
                $"O download foi interrompido: {recebido / 1024d / 1024d:N1} MB de {total / 1024d / 1024d:N1} MB. " +
                "Verifique a conexão e tente novamente.");
        }
    }

    private static void GarantirPermissaoDeEscrita(string pastaInstalacao)
    {
        var teste = Path.Combine(pastaInstalacao, $".permissao_{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(teste, string.Empty);
            File.Delete(teste);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw new UnauthorizedAccessException(
                $"O sistema não tem permissão para gravar em {pastaInstalacao}. " +
                "Execute o aplicativo como administrador para atualizar, ou instale-o em uma pasta do usuário.");
        }
    }

    /// <summary>
    /// O zip pode ter os arquivos na raiz ou dentro de uma pasta, dependendo de como foi
    /// compactado. Encontra o nível onde o executável está — e falha aqui, antes de qualquer
    /// arquivo ser trocado, se o pacote não for o que se espera.
    /// </summary>
    internal static string ResolverRaizDoPacote(string pastaExtracao)
    {
        if (File.Exists(Path.Combine(pastaExtracao, NomeExecutavel)))
            return pastaExtracao;

        var aninhada = Directory.GetDirectories(pastaExtracao)
            .FirstOrDefault(d => File.Exists(Path.Combine(d, NomeExecutavel)));

        if (aninhada is not null)
            return aninhada;

        throw new InvalidDataException(
            $"O pacote baixado não contém {NomeExecutavel} e não pode ser aplicado. " +
            "A release provavelmente foi publicada com o arquivo errado.");
    }

    internal sealed class ReleaseGitHub
    {
        [JsonPropertyName("tag_name")]
        public string Tag { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Nome { get; set; }

        [JsonPropertyName("body")]
        public string? Corpo { get; set; }

        [JsonPropertyName("html_url")]
        public string? UrlPagina { get; set; }

        [JsonPropertyName("assets")]
        public List<ArquivoGitHub>? Arquivos { get; set; }
    }

    internal sealed class ArquivoGitHub
    {
        [JsonPropertyName("name")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string UrlDownload { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Tamanho { get; set; }
    }
}
