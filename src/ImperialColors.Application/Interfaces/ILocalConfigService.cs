namespace ImperialColors.Application.Interfaces;

public interface ILocalConfigService
{
    string? ImpressoraSelecionada { get; set; }
    void Carregar();
    Task SalvarAsync();
}
