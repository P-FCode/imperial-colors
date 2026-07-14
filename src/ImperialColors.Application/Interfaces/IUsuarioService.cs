using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioDto>> ObterTodosAsync();
    Task<UsuarioDto?> ObterPorIdAsync(Guid id);
    Task<UsuarioDto> CriarPorAdminAsync(CriarUsuarioAdminDto dto);
    Task<UsuarioDto> AtualizarPorAdminAsync(AtualizarUsuarioAdminDto dto);
    Task CancelarAsync(Guid id);
    Task AprovarAsync(Guid id);
    Task ExcluirFisicamenteAsync(Guid id, Guid usuarioLogadoId);
}
