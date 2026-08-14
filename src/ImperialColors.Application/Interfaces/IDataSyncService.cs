namespace ImperialColors.Application.Interfaces;

public interface IDataSyncService
{
    Task<int> SincronizarPendentesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disparado quando o número de vendas aguardando sincronização muda, com o total que
    /// restou. Permite a tela do PDV mostrar "3 vendas a sincronizar" e vê-lo zerar sozinho,
    /// em vez de consultar o contador em laço.
    ///
    /// Vem de uma thread de segundo plano (o laço do serviço): quem for tocar em UI precisa
    /// voltar para o dispatcher.
    /// </summary>
    event EventHandler<int>? PendentesAlterado;
}
