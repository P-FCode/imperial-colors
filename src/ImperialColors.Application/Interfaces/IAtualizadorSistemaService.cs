using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Atualização do sistema a partir das Releases do GitHub.
///
/// O serviço vai até o ponto de deixar tudo pronto e disparar o processo auxiliar que troca
/// os arquivos; ele NÃO encerra o aplicativo. Quem encerra é a UI, logo depois de
/// <see cref="BaixarEPrepararAsync"/> retornar — a troca só pode acontecer com o executável
/// liberado, e decidir a hora de fechar a janela é responsabilidade da camada de tela.
/// </summary>
public interface IAtualizadorSistemaService
{
    /// <summary>Versão desta instalação, no formato <c>v1.2.3</c>.</summary>
    string VersaoInstaladaTexto { get; }

    Task<ResultadoVerificacaoAtualizacaoDto> VerificarAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Baixa o pacote da release, confere a integridade, extrai e deixa o processo auxiliar
    /// aguardando o fechamento deste aplicativo para aplicar a troca e reabrir.
    /// </summary>
    Task BaixarEPrepararAsync(
        ResultadoVerificacaoAtualizacaoDto verificacao,
        IProgress<ProgressoAtualizacaoDto>? progresso = null,
        CancellationToken cancellationToken = default);
}
