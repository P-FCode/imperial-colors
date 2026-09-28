namespace ImperialColors.Infrastructure.Configuration;

/// <summary>
/// Repositório do GitHub de onde o sistema instalado busca as releases para se atualizar.
///
/// Ficava escrito no código, em duas constantes. O problema disso só aparece no dia de
/// trocar o projeto de conta: a correção que aponta para o repositório novo só chega ao
/// cliente através de uma release — publicada no repositório ANTIGO, que ele ainda consulta.
/// Se o antigo saísse do ar antes de todas as máquinas atualizarem, elas ficavam sem
/// caminho de atualização e só voltavam com reinstalação manual, PC por PC.
///
/// Com o valor no <c>.env</c>, a troca é uma linha no arquivo ao lado do executável, sem
/// depender de release nenhuma.
/// </summary>
public sealed class AtualizacaoConfig
{
    public const string Variavel = "ATUALIZACAO_REPO";

    public const string ProprietarioPadrao = "P-FCode";
    public const string RepositorioPadrao = "imperial-colors";

    public string Proprietario { get; init; } = ProprietarioPadrao;
    public string Repositorio { get; init; } = RepositorioPadrao;

    /// <summary>Como o GitHub mostra: <c>dono/repositorio</c>.</summary>
    public string Caminho => $"{Proprietario}/{Repositorio}";

    /// <summary>Verdadeiro quando o valor veio do <c>.env</c>; falso quando caiu no padrão —
    /// usado para registrar em log qual repositório está valendo.</summary>
    public bool ConfiguradoPeloAmbiente { get; init; }

    public static AtualizacaoConfig CarregarDoAmbiente()
        => Interpretar(Environment.GetEnvironmentVariable(Variavel));

    /// <summary>
    /// Aceita o formato <c>dono/repositorio</c>, que é como o endereço aparece no GitHub e
    /// como a pessoa vai copiar da barra do navegador. Valor vazio ou fora do formato cai no
    /// padrão: um erro de digitação aqui não pode deixar o cliente sem atualização nenhuma —
    /// quem publica o sistema confere pelo log qual repositório ficou valendo.
    /// </summary>
    public static AtualizacaoConfig Interpretar(string? valor)
    {
        var partes = (valor ?? string.Empty)
            .Trim()
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (partes.Length != 2)
            return new AtualizacaoConfig();

        return new AtualizacaoConfig
        {
            Proprietario = partes[0],
            Repositorio = partes[1],
            ConfiguradoPeloAmbiente = true
        };
    }
}
