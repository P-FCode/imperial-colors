using ImperialColors.Application.Interfaces;

namespace ImperialColors.Application.Services;

/// <summary>Padrão fora da interface gráfica (testes, rotinas); a UI registra a sessão logada no lugar.</summary>
public sealed class UsuarioAtualSistema : IUsuarioAtual
{
    public string Nome => "Sistema";
}
