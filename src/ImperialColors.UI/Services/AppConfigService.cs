using ImperialColors.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO;

namespace ImperialColors.UI.Services;

public class AppConfigService : IAppConfigService
{
    private readonly IOptionsMonitor<EmpresaConfig> _empresa;

    public AppConfigService(IOptionsMonitor<EmpresaConfig> empresa, IConfiguration configuration)
    {
        _empresa = empresa;
        ConnectionString = MontarConnectionString();
        CupomRodape = ObterCupomRodape(configuration);

        IconPath = ResolverCaminhoRecurso(Obter("ICON_PATH", "icons/logoimperialcolors.ico"));
        LogoPath = ResolverCaminhoRecurso(Obter("LOGO_PATH", "icons/logoimperialcolors.png"));
        LogoSemFundoPath = ResolverCaminhoRecurso(Obter("LOGO_SEM_FUNDO_PATH", "icons/logoimperialcolors-nobg.png"));
        BackupPath = Obter("BACKUP_PATH", @"C:\backup_sistema");
    }

    public string ConnectionString { get; }

    public string EmpresaNome => _empresa.CurrentValue.NomeFantasia;

    public string EmpresaSubtitulo => _empresa.CurrentValue.Subtitulo;

    public string EmpresaRazaoSocial => _empresa.CurrentValue.RazaoSocial;

    public string EmpresaTelefone => _empresa.CurrentValue.Telefone;

    public string EmpresaEmail => Obter("EMPRESA_EMAIL", string.Empty);

    public string EmpresaEndereco => _empresa.CurrentValue.Endereco;

    public string EmpresaCnpj => _empresa.CurrentValue.CNPJ;

    public string CupomRodape { get; }

    public string BackupPath { get; }

    public string IconPath { get; }

    public string LogoPath { get; }

    public string LogoSemFundoPath { get; }

    public EmpresaConfig Empresa => _empresa.CurrentValue;

    public string ResolverCaminhoRecurso(string caminhoRelativo)
    {
        if (Path.IsPathRooted(caminhoRelativo) && File.Exists(caminhoRelativo))
            return caminhoRelativo;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidatos = new[]
        {
            Path.Combine(baseDir, caminhoRelativo),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", caminhoRelativo))
        };

        return candidatos.FirstOrDefault(File.Exists) ?? candidatos[0];
    }

    private static string ObterCupomRodape(IConfiguration configuration)
    {
        var env = Environment.GetEnvironmentVariable("CUPOM_MENSAGEM_RODAPE")?.Trim();
        if (!string.IsNullOrEmpty(env))
            return env;

        var appsettings = configuration["Cupom:MensagemRodape"]?.Trim();
        if (!string.IsNullOrEmpty(appsettings))
            return appsettings;

        return "Obrigado pela preferência!";
    }

    internal static string MontarConnectionString()
    {
        var host = Obter("DB_HOST", "localhost");
        var port = Obter("DB_PORT", "5432");
        var database = Obter("DB_NAME", "imperial_colors");
        var username = Obter("DB_USER", "postgres");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
        var sslMode = Obter("DB_SSL_MODE", "Prefer");

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode};Trust Server Certificate=true;";
    }

    private static string Obter(string chave, string padrao)
        => Environment.GetEnvironmentVariable(chave)?.Trim() is { Length: > 0 } valor ? valor : padrao;
}
