using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Data;

public static class UsuarioDatabaseSeeder
{
    public static async Task SeedAdminAsync(AppDbContext context, ILogger logger)
    {
        var nome = Environment.GetEnvironmentVariable("ADMIN_NOME")?.Trim() ?? "Administrador";
        var username = InputSanitizer.SanitizarUsername(
            Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin");
        var email = InputSanitizer.SanitizarEmail(
            Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@imperialcolors.local");
        var senha = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        var resetPassword = string.Equals(
            Environment.GetEnvironmentVariable("ADMIN_RESET_PASSWORD"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(username))
        {
            logger.LogWarning("Seed de admin ignorado: ADMIN_USERNAME inválido.");
            return;
        }

        var existente = await context.Usuarios
            .FirstOrDefaultAsync(u => u.Username == username);

        if (existente is not null)
        {
            var alterado = false;

            if (existente.Status != StatusUsuario.Aprovado)
            {
                existente.Status = StatusUsuario.Aprovado;
                alterado = true;
            }

            if (existente.Permissao != PermissaoUsuario.Admin)
            {
                existente.Permissao = PermissaoUsuario.Admin;
                alterado = true;
            }

            if (resetPassword && !string.IsNullOrWhiteSpace(senha) && InputSanitizer.SenhaForte(senha))
            {
                var (hash, salt) = PasswordHasher.HashPassword(senha);
                existente.SenhaHash = hash;
                existente.Salt = salt;
                alterado = true;
                logger.LogInformation("Senha do administrador redefinida conforme ADMIN_PASSWORD no .env.");
            }

            if (alterado)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Conta administrador garantida: {Username}", username);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning(
                "Seed de admin ignorado: defina ADMIN_PASSWORD no .env para criar o usuário administrador inicial.");
            return;
        }

        if (!InputSanitizer.SenhaForte(senha))
        {
            logger.LogWarning(
                "Seed de admin ignorado: ADMIN_PASSWORD deve ter 8+ caracteres com maiúscula, minúscula e número.");
            return;
        }

        var (novoHash, novoSalt) = PasswordHasher.HashPassword(senha);

        var admin = new Usuario
        {
            Id = Guid.NewGuid(),
            NomeCompleto = InputSanitizer.SanitizarTexto(nome, 200),
            Username = username,
            Email = email,
            SenhaHash = novoHash,
            Salt = novoSalt,
            Status = StatusUsuario.Aprovado,
            Permissao = PermissaoUsuario.Admin,
            DataCadastro = DateTime.UtcNow
        };

        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Usuário administrador inicial criado: {Username}", admin.Username);
    }
}
