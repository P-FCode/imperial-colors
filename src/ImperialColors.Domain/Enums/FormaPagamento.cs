namespace ImperialColors.Domain.Enums;

public enum FormaPagamento
{
    Dinheiro = 1,
    CartaoDebito = 2,
    CartaoCredito = 3,
    Pix = 4,
    Boleto = 5,

    /// <summary>Nota fiscal sem pagamento associado (ex.: transferência entre filiais,
    /// amostra grátis, brinde) — mapeia para tPag="90" (Sem Pagamento) da NF-e, que
    /// dispensa o valor do pagamento (vPag). Só faz sentido em notas fiscais avulsas; não
    /// aparece na tela de PDV, que usa uma lista própria de botões — nunca tem sentido
    /// "vender" sem receber nada.</summary>
    SemPagamento = 6
}
