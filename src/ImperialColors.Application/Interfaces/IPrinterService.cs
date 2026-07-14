namespace ImperialColors.Application.Interfaces;

public interface IPrinterService
{
    IReadOnlyList<string> ListarImpressoras();
    string? ObterImpressoraPadraoSistema();
    bool ImpressoraExiste(string nomeImpressora);
}
