namespace ImperialColors.Application.Interfaces;

public interface IBackupService
{
    /// <summary>
    /// Verifica se o backup está vencido e, se necessário, executa em Task de segundo plano.
    /// Nunca bloqueia a thread chamadora.
    /// </summary>
    void IniciarVerificacaoEmSegundoPlano();
}
