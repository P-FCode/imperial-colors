using ImperialColors.Application.Helpers;
using Xunit;

namespace ImperialColors.Application.Tests;

public class BackupScheduleHelperTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(10, true)]
    public void DeveExecutarBackup_RespeitaIntervaloDeSeteDias(int? diasAtras, bool esperado)
    {
        var hoje = new DateTime(2026, 6, 20);
        DateTime? ultimo = diasAtras.HasValue ? hoje.AddDays(-diasAtras.Value) : null;

        var resultado = BackupScheduleHelper.DeveExecutarBackup(ultimo, hoje);

        Assert.Equal(esperado, resultado);
    }
}

public class BackupPathHelperTests
{
    [Fact]
    public void MontarPastaDestino_DeveUsarMesAnoEPastaDoDia()
    {
        var data = new DateTime(2026, 6, 20);
        var caminho = BackupPathHelper.MontarPastaDestino(@"C:\backup_sistema", data);

        Assert.Equal(@"C:\backup_sistema\junho-2026\20-06-2026", caminho);
    }

    [Fact]
    public void MontarNomeArquivoSql_DeveGerarPrefixoComData()
    {
        var data = new DateTime(2026, 6, 20);
        var nome = BackupPathHelper.MontarNomeArquivoSql("Imperial Colors", data);

        Assert.Equal("backup_imperialcolors_20_06_2026.sql", nome);
    }
}
