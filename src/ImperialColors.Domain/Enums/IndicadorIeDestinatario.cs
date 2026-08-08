namespace ImperialColors.Domain.Enums;

/// <summary>
/// Indicador da Inscrição Estadual do destinatário (tag <c>indIEDest</c> da NF-e) — os
/// valores numéricos são os códigos oficiais da SEFAZ, não arbitrários. Define se a nota
/// deve exigir/validar a IE do cliente. A maioria dos clientes de balcão (pessoa física)
/// é "Não Contribuinte".
/// </summary>
public enum IndicadorIeDestinatario
{
    ContribuinteIcms = 1,
    Isento = 2,
    NaoContribuinte = 9
}
