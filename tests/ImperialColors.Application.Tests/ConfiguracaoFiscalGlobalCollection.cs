using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// Testes de integração que leem/gravam a configuração fiscal GLOBAL da empresa
/// (regime tributário em ParametroSistema, ou a linha singleton de
/// ConfiguracaoFiscalEmpresa) precisam rodar em série entre si — nunca em paralelo.
/// Sem isso, um teste que restaura o padrão no `finally` pode sobrescrever a
/// configuração que outro teste acabou de gravar um instante antes de ler de volta
/// (xUnit por padrão paraleliza classes de teste diferentes). Marque a classe com
/// <c>[Collection(Nome)]</c> para entrar nesse grupo.
/// </summary>
[CollectionDefinition(Nome)]
public class ConfiguracaoFiscalGlobalCollection
{
    public const string Nome = "ConfiguracaoFiscalGlobal";
}
