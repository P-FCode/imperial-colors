using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IViaCepService
{
    Task<EnderecoViaCepDto?> ConsultarAsync(string cep, CancellationToken cancellationToken = default);
}
