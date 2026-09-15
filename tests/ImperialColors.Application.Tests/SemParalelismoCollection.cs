using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Classes que dependem de estado global do processo ou do banco inteiro (variável de ambiente,
/// contagem total de vendas) e por isso falham de forma intermitente quando outras classes rodam
/// ao mesmo tempo. O xUnit executa esta coleção sozinha, depois das coleções paralelas.
/// </summary>
[CollectionDefinition(Nome, DisableParallelization = true)]
public class SemParalelismoCollection
{
    public const string Nome = "SemParalelismo";
}
