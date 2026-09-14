using ImperialColors.Application.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ImperialColors.UI.Services;

public class AppConfigService : IAppConfigService
{
    private readonly IConfiguration _configuration;
    private EmpresaConfig _empresa;

    public AppConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
        _empresa = MontarEmpresa();
    }

    public event EventHandler? ConfiguracoesAlteradas;

    // Propriedades calculadas a cada leitura (e não fixadas no construtor) para que salvar
    // pela tela de Configurações valha na hora. A conta é trivial — leitura de variável de
    // ambiente — e ninguém consulta isso em laço quente.
    public string ConnectionString => MontarConnectionString();

    public string EmpresaNome => _empresa.NomeFantasia;

    public string EmpresaSubtitulo => _empresa.Subtitulo;

    public string EmpresaRazaoSocial => _empresa.RazaoSocial;

    public string EmpresaTelefone => _empresa.Telefone;

    public string EmpresaEmail => _empresa.Email;

    public string EmpresaEndereco => _empresa.Endereco;

    public string EmpresaCnpj => _empresa.CNPJ;

    public string CupomRodape => ObterCupomRodape(_configuration);

    public string BackupPath => Obter("BACKUP_PATH", @"C:\backup_sistema");

    public string IconPath => ResolverCaminhoRecurso(Obter("ICON_PATH", "icons/logoimperialcolors.ico"));

    public string LogoPath => ResolverCaminhoRecurso(Obter("LOGO_PATH", "icons/logoimperialcolors.png"));

    public string LogoSemFundoPath => ResolverCaminhoRecurso(Obter("LOGO_SEM_FUNDO_PATH", "icons/logoimperialcolors-nobg.png"));

    public EmpresaConfig Empresa => _empresa;

    public void Recarregar()
    {
        _empresa = MontarEmpresa();
        ConfiguracoesAlteradas?.Invoke(this, EventArgs.Empty);
    }

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

    /// <summary>
    /// Reproduz a mesma precedência da inicialização (defaults → appsettings.json → .env).
    /// Antes isso vinha de <c>IOptionsMonitor</c>, que só reavalia os overrides de ambiente
    /// quando o JSON muda — ou seja, nunca enxergaria um .env reescrito pela própria tela.
    /// </summary>
    private EmpresaConfig MontarEmpresa()
    {
        var empresa = new EmpresaConfig();
        _configuration.GetSection(EmpresaConfig.Secao).Bind(empresa);
        EmpresaConfigEnvironmentOverrides.Aplicar(empresa);
        return empresa;
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
