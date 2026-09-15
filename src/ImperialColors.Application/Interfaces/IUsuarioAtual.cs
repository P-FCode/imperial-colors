namespace ImperialColors.Application.Interfaces;

/// <summary>Quem está operando o sistema — usado para registrar na auditoria o autor de cada ação.</summary>
public interface IUsuarioAtual
{
    string Nome { get; }
}
