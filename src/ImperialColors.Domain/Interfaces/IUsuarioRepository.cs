using ImperialColors.Domain.Entities;

namespace ImperialColors.Domain.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id);
    Task<Usuario?> ObterPorUsernameAsync(string username);
    Task<Usuario?> ObterPorEmailAsync(string email);
    Task<IEnumerable<Usuario>> ObterTodosAsync();
    Task<Usuario> AdicionarAsync(Usuario usuario);
    Task<Usuario> AtualizarAsync(Usuario usuario);
    Task<bool> UsernameExisteAsync(string username, Guid? excluirId = null);
    Task<bool> EmailExisteAsync(string email, Guid? excluirId = null);
    Task<bool> ExisteAdminAsync();
    Task<int> ContarAdminsAprovadosAsync(Guid? excluirId = null);
    Task RemoverFisicamenteAsync(Guid id);
}
