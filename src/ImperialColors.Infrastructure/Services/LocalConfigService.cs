using ImperialColors.Application.Interfaces;
using System.Text.Json;

namespace ImperialColors.Infrastructure.Services;

public class LocalConfigService : ILocalConfigService
{
    private readonly string _caminhoArquivo;
    private LocalConfigModel _config = new();

    public LocalConfigService()
    {
        _caminhoArquivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "localsettings.json");
        Carregar();
    }

    public string? ImpressoraSelecionada
    {
        get => _config.ImpressoraSelecionada;
        set => _config.ImpressoraSelecionada = value;
    }

    public void Carregar()
    {
        try
        {
            if (!File.Exists(_caminhoArquivo))
            {
                _config = new LocalConfigModel();
                return;
            }

            var json = File.ReadAllText(_caminhoArquivo);
            _config = JsonSerializer.Deserialize<LocalConfigModel>(json) ?? new LocalConfigModel();
        }
        catch
        {
            _config = new LocalConfigModel();
        }
    }

    public async Task SalvarAsync()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_caminhoArquivo, json);
    }

    private sealed class LocalConfigModel
    {
        public string? ImpressoraSelecionada { get; set; }
    }
}
