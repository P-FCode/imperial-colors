using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IAuthService
{
    Task<UsuarioSessaoDto> LoginAsync(LoginDto dto);
    Task<UsuarioDto> RegistrarAsync(CadastroUsuarioDto dto);
}
