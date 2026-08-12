namespace ImperialColors.Domain.Enums;

/// <summary>
/// Tipo/modelo da nota fiscal emitida por este módulo. O valor numérico é o próprio
/// código do modelo SEFAZ (tag <c>ide.mod</c> do XML) — não é arbitrário, é usado
/// diretamente ao montar o payload da API Fiscal.
/// </summary>
public enum TipoNotaFiscal
{
    /// <summary>NF-e — Nota Fiscal Eletrônica (B2B/Atacado).</summary>
    NFe = 55,

    /// <summary>NFC-e — Nota Fiscal de Consumidor Eletrônica (PDV/Consumidor).</summary>
    NFCe = 65
}
