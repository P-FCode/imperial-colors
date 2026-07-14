using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface ICnpjConsultaService
{
    Task<DadosCnpjDto?> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}
