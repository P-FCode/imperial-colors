using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UsuarioRepository(IDbContextFactory<AppDbContext> contextFactory)
        => _contextFactory = contextFactory;

    public async Task<Usuario?> ObterPorIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> ObterPorUsernameAsync(string username)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant());
    }

    public async Task<Usuario?> ObterPorEmailAsync(string email)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<IEnumerable<Usuario>> ObterTodosAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.OrderBy(u => u.NomeCompleto).ToListAsync();
    }

    public async Task<bool> UsernameExisteAsync(string username, Guid? excluirId = null)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.AnyAsync(u =>
            u.Username == username.ToLowerInvariant() && (!excluirId.HasValue || u.Id != excluirId));
    }

    public async Task<bool> EmailExisteAsync(string email, Guid? excluirId = null)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.AnyAsync(u =>
            u.Email == email.ToLowerInvariant() && (!excluirId.HasValue || u.Id != excluirId));
    }

    public async Task<bool> ExisteAdminAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.AnyAsync(u =>
            u.Permissao == Domain.Enums.PermissaoUsuario.Admin &&
            u.Status == Domain.Enums.StatusUsuario.Aprovado);
    }

    public async Task<Usuario> AdicionarAsync(Usuario usuario)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }

    public async Task<Usuario> AtualizarAsync(Usuario usuario)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Usuarios.Update(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }

    public async Task<int> ContarAdminsAprovadosAsync(Guid? excluirId = null)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Usuarios.CountAsync(u =>
            u.Permissao == Domain.Enums.PermissaoUsuario.Admin &&
            u.Status == Domain.Enums.StatusUsuario.Aprovado &&
            (!excluirId.HasValue || u.Id != excluirId));
    }

    public async Task RemoverFisicamenteAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            return;

        context.Usuarios.Remove(usuario);
        await context.SaveChangesAsync();
    }
}
