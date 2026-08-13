using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Busca de NCM (Nomenclatura Comum do Mercosul) por código ou palavra-chave da descrição —
/// alimenta o autocompletar da tela de cadastro do produto. Consulta externa auxiliar: se
/// falhar (sem internet, serviço fora do ar), o operador ainda pode digitar o NCM manualmente
/// — nunca deve travar o cadastro.
/// </summary>
public interface INcmService
{
    /// <summary>Retorna só códigos de 8 dígitos vigentes (folhas da tabela — os níveis
    /// intermediários de capítulo/posição/subposição são filtrados) cujo código ou descrição
    /// contém <paramref name="termo"/>. Lista vazia em caso de termo curto demais, sem
    /// resultado, ou falha na consulta — nunca lança exceção para o chamador.</summary>
    Task<IReadOnlyList<NcmSugestaoDto>> BuscarAsync(string termo, CancellationToken cancellationToken = default);
}
