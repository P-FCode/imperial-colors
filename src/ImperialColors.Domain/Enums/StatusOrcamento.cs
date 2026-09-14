namespace ImperialColors.Domain.Enums;

/// <summary>
/// Situação comercial de um orçamento. Não existe "Expirado" aqui de propósito: vencimento é
/// consequência da data de validade, e gravar esse estado exigiria alguém varrendo a tabela
/// todo dia para mantê-lo verdadeiro.
/// </summary>
public enum StatusOrcamento
{
    Aberto = 1,
    Aprovado = 2,
    Recusado = 3
}
