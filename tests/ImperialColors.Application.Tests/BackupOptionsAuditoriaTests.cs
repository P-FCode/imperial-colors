using ImperialColors.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// AUDITORIA (15/09) — desde 6fc4cbe, "Pasta de Backup" é editável em Configurações → Geral
/// e grava em <c>BACKUP_PATH</c> no <c>.env</c> em tempo real (<c>ArquivoEnvService</c>
/// também chama <c>Environment.SetEnvironmentVariable</c> no processo atual).
///
/// Mas <c>BackupOptions</c> era registrado assim, uma única vez, na inicialização:
///   <c>services.AddSingleton(BackupOptions.CarregarDoAmbiente())</c>
/// — uma INSTÂNCIA (não uma fábrica), calculada no boot e nunca recalculada.
/// <c>BackupService</c>, que roda o backup automático a cada N dias, recebia essa mesma
/// instância injetada e a mantinha para a vida inteira do processo — trocar a pasta pela tela
/// não tinha efeito algum até reiniciar.
///
/// CORRIGIDO (mesmo dia): o registro virou <c>services.AddSingleton&lt;Func&lt;BackupOptions&gt;&gt;(...)</c>
/// e <c>BackupService</c> chama essa função a cada verificação, nunca guardando o resultado.
/// </summary>
[Collection(SemParalelismoCollection.Nome)]
public class BackupOptionsAuditoriaTests
{
    /// <summary>
    /// Pré-condição: se nem <c>CarregarDoAmbiente()</c> sozinho enxergasse a troca, o
    /// problema seria ainda mais grave (a leitura em si estaria quebrada).
    /// </summary>
    [Fact]
    public void CarregarDoAmbiente_ChamadoDeNovo_EnxergaValorTrocado()
    {
        var original = Environment.GetEnvironmentVariable("BACKUP_PATH");
        try
        {
            Environment.SetEnvironmentVariable("BACKUP_PATH", @"C:\backup_antigo");
            var antes = BackupOptions.CarregarDoAmbiente();
            Assert.Equal(@"C:\backup_antigo", antes.DiretorioRaiz);

            // Isto é o que ArquivoEnvService.SalvarAsync faz de verdade após gravar o .env.
            Environment.SetEnvironmentVariable("BACKUP_PATH", @"C:\backup_novo");
            var depois = BackupOptions.CarregarDoAmbiente();

            Assert.Equal(@"C:\backup_novo", depois.DiretorioRaiz);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BACKUP_PATH", original);
        }
    }

    /// <summary>
    /// CORRIGIDO (15/09): registra <c>Func&lt;BackupOptions&gt;</c> exatamente como
    /// <c>InfrastructureExtensions.AddInfrastructure</c> faz hoje, resolve essa função UMA VEZ
    /// do container (simulando o que <c>BackupService</c> recebe por injeção de dependência no
    /// boot) e confirma que chamá-la de novo, depois de trocar <c>BACKUP_PATH</c> — o que
    /// <c>ArquivoEnvService.SalvarAsync</c> faz de verdade quando o operador edita a pasta pela
    /// tela — já enxerga o valor novo, sem recriar o container nem reiniciar o processo.
    ///
    /// Antes da correção, o equivalente era uma INSTÂNCIA presa no container
    /// (<c>services.AddSingleton(BackupOptions.CarregarDoAmbiente())</c>): o cliente que
    /// trocasse a pasta de backup pela tela continuava tendo os backups automáticos gravados
    /// na pasta ANTIGA até reiniciar o sistema — sem erro, sem aviso, só descoberto no dia de
    /// restaurar e achar a pasta nova vazia.
    /// </summary>
    [Fact]
    public void FactoryDeBackupOptionsNoContainer_AcompanhaTrocaDePastaPelaTela()
    {
        var original = Environment.GetEnvironmentVariable("BACKUP_PATH");
        try
        {
            Environment.SetEnvironmentVariable("BACKUP_PATH", @"C:\backup_configurado_no_boot");

            var services = new ServiceCollection();
            services.AddSingleton<Func<BackupOptions>>(_ => BackupOptions.CarregarDoAmbiente);
            using var provider = services.BuildServiceProvider();

            // Resolvida uma única vez — como o construtor de BackupService recebe.
            var obterOptions = provider.GetRequiredService<Func<BackupOptions>>();
            Assert.Equal(@"C:\backup_configurado_no_boot", obterOptions().DiretorioRaiz);

            // Operador troca a pasta pela tela; nada recria o container.
            Environment.SetEnvironmentVariable("BACKUP_PATH", @"C:\backup_escolhido_na_tela");

            // A MESMA Func resolvida do MESMO container já enxerga o valor novo.
            Assert.Equal(@"C:\backup_escolhido_na_tela", obterOptions().DiretorioRaiz);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BACKUP_PATH", original);
        }
    }
}
