using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Entities;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public StatusUsuario Status { get; set; } = StatusUsuario.AguardandoAprovacao;
    public PermissaoUsuario Permissao { get; set; } = PermissaoUsuario.Caixa;
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}
