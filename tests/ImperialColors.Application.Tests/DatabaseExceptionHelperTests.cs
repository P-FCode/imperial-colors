using ImperialColors.Infrastructure.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class DatabaseExceptionHelperTests
{
    [Fact]
    public void EhViolacaoUnicidadeCodigoInterno_DeveDetectarIndicePostgres()
    {
        var ex = new Exception("23505: duplicate key value violates unique constraint \"IX_produtos_codigo_interno\"");
        Assert.True(DatabaseExceptionHelper.EhViolacaoUnicidadeCodigoInterno(ex));
    }

    [Fact]
    public void EhViolacaoUnicidadeNumeracaoNotaFiscal_DeveDetectarIndicePostgres()
    {
        var ex = new Exception("23505: duplicate key value violates unique constraint \"IX_notas_fiscais_tipo_serie_numero\"");
        Assert.True(DatabaseExceptionHelper.EhViolacaoUnicidadeNumeracaoNotaFiscal(ex));
    }

    [Fact]
    public void EhViolacaoUnicidadeNumeracaoNotaFiscal_IgnoraOutrasViolacoesDeUnicidade()
    {
        var ex = new Exception("23505: duplicate key value violates unique constraint \"IX_produtos_codigo_interno\"");
        Assert.False(DatabaseExceptionHelper.EhViolacaoUnicidadeNumeracaoNotaFiscal(ex));
    }
}
