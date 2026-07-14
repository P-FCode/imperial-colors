using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Configuration;
using ImperialColors.Infrastructure.Services.Backup;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly IParametroSistemaRepository _parametroRepository;
    private readonly BackupOptions _options;
    private readonly ILogger<BackupService> _logger;
    private int _executando;

    public BackupService(
        IParametroSistemaRepository parametroRepository,
        BackupOptions options,
        ILogger<BackupService> logger)
    {
        _parametroRepository = parametroRepository;
        _options = options;
        _logger = logger;
    }

    public void IniciarVerificacaoEmSegundoPlano()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await VerificarEExecutarSeNecessarioAsync();
            }
            catch (Exception ex)
            {
                BackupLogWriter.Registrar(_options.DiretorioRaiz, "Falha inesperada na rotina de backup.", ex);
                _logger.LogError(ex, "Falha inesperada na rotina de backup.");
            }
        });
    }

    internal async Task VerificarEExecutarSeNecessarioAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _executando, 1, 0) != 0)
            return;

        try
        {
            var hoje = DateTime.Today;
            var ultimoBackup = await _parametroRepository.ObterDataAsync(
                ParametroSistemaChaves.DataUltimoBackup,
                cancellationToken);

            if (!BackupScheduleHelper.DeveExecutarBackup(ultimoBackup, hoje, _options.IntervaloDias))
            {
                _logger.LogDebug("Backup automático não necessário. Último: {UltimoBackup}", ultimoBackup);
                return;
            }

            _logger.LogInformation("Iniciando backup híbrido automático.");

            try
            {
                await ExecutarBackupCompletoAsync(hoje, cancellationToken);
                await _parametroRepository.SalvarDataAsync(
                    ParametroSistemaChaves.DataUltimoBackup,
                    hoje,
                    cancellationToken);
                _logger.LogInformation("Backup automático concluído com sucesso.");
            }
            catch (Exception ex)
            {
                BackupLogWriter.Registrar(_options.DiretorioRaiz, "Falha ao executar backup híbrido.", ex);
                _logger.LogError(ex, "Falha ao executar backup híbrido.");
            }
        }
        finally
        {
            Interlocked.Exchange(ref _executando, 0);
        }
    }

    internal async Task ExecutarBackupCompletoAsync(DateTime dataExecucao, CancellationToken cancellationToken = default)
    {
        var pastaDestino = BackupPathHelper.MontarPastaDestino(_options.DiretorioRaiz, dataExecucao);
        Directory.CreateDirectory(pastaDestino);

        var nomeSql = BackupPathHelper.MontarNomeArquivoSql(_options.PrefixoEmpresa, dataExecucao);
        var caminhoSql = Path.Combine(pastaDestino, nomeSql);

        var pgDump = PgDumpExecutor.LocalizarPgDump(_options.PgDumpPath)
            ?? throw new InvalidOperationException(
                "Utilitário pg_dump não encontrado. Configure PG_DUMP_PATH no .env ou instale o PostgreSQL.");

        await PgDumpExecutor.ExecutarAsync(
            pgDump,
            _options.Host,
            _options.Porta,
            _options.Usuario,
            _options.Senha,
            _options.Banco,
            caminhoSql,
            cancellationToken);

        CopiarArquivosLocais(pastaDestino);
    }

    private void CopiarArquivosLocais(string pastaDestino)
    {
        if (File.Exists(_options.CaminhoAppsettings))
        {
            File.Copy(_options.CaminhoAppsettings, Path.Combine(pastaDestino, "appsettings.json"), overwrite: true);
        }
        else
        {
            throw new FileNotFoundException("Arquivo appsettings.json não encontrado para backup.", _options.CaminhoAppsettings);
        }

        if (!Directory.Exists(_options.PastaLogos))
            throw new DirectoryNotFoundException($"Pasta de logos não encontrada: {_options.PastaLogos}");

        var destinoLogos = Path.Combine(pastaDestino, "logos_empresa");
        CopiarDiretorio(_options.PastaLogos, destinoLogos);
    }

    private static void CopiarDiretorio(string origem, string destino)
    {
        Directory.CreateDirectory(destino);

        foreach (var arquivo in Directory.GetFiles(origem))
        {
            var nome = Path.GetFileName(arquivo);
            File.Copy(arquivo, Path.Combine(destino, nome), overwrite: true);
        }

        foreach (var subpasta in Directory.GetDirectories(origem))
        {
            var nome = Path.GetFileName(subpasta);
            CopiarDiretorio(subpasta, Path.Combine(destino, nome));
        }
    }
}
