namespace ImperialColors.Application.DTOs;

/// <summary>
/// Resultado da consulta à última release publicada no GitHub.
///
/// Separa três situações que a tela precisa tratar de formas diferentes e que um simples
/// <c>bool</c> confundiria: a consulta falhou (<see cref="Sucesso"/> falso — rede, API fora,
/// limite de requisições), a consulta funcionou e não há nada novo, e a consulta funcionou e
/// existe uma versão nova pronta para baixar.
/// </summary>
public sealed class ResultadoVerificacaoAtualizacaoDto
{
    public bool Sucesso { get; init; }
    public bool AtualizacaoDisponivel { get; init; }

    public Version VersaoInstalada { get; init; } = new(0, 0, 0);
    public Version VersaoPublicada { get; init; } = new(0, 0, 0);

    public string NomeRelease { get; init; } = string.Empty;
    public string NotasRelease { get; init; } = string.Empty;
    public string UrlRelease { get; init; } = string.Empty;

    public string? UrlDownload { get; init; }
    public string? NomeArquivo { get; init; }
    public long TamanhoBytes { get; init; }

    /// <summary>
    /// Preenchida tanto em falha quanto no caso "existe release nova, mas ela não tem o
    /// pacote anexado" — que é sucesso de consulta e mesmo assim impede atualizar.
    /// </summary>
    public string? MensagemErro { get; init; }

    public string VersaoInstaladaTexto => FormatarVersao(VersaoInstalada);
    public string VersaoPublicadaTexto => FormatarVersao(VersaoPublicada);

    public string TamanhoTexto => TamanhoBytes <= 0
        ? "tamanho desconhecido"
        : $"{TamanhoBytes / 1024d / 1024d:N1} MB";

    public static ResultadoVerificacaoAtualizacaoDto Falha(string mensagem) =>
        new() { Sucesso = false, MensagemErro = mensagem };

    private static string FormatarVersao(Version v) =>
        $"v{v.Major}.{v.Minor}.{Math.Max(v.Build, 0)}";
}

/// <summary>Etapa corrente da atualização, para a barra de progresso da tela.</summary>
public sealed class ProgressoAtualizacaoDto
{
    public required string Etapa { get; init; }

    /// <summary>0 a 100 durante o download; nulo nas etapas sem percentual mensurável.</summary>
    public double? Percentual { get; init; }
}
