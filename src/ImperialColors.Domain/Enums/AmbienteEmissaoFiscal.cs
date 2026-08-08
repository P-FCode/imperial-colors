namespace ImperialColors.Domain.Enums;

/// <summary>
/// Ambiente de emissão de NF-e/NFC-e (tag <c>tpAmb</c> do XML). Os valores numéricos
/// seguem exatamente o código oficial da SEFAZ — não são arbitrários.
/// Homologação = testes, sem valor fiscal. Produção = nota fiscal real.
/// </summary>
public enum AmbienteEmissaoFiscal
{
    Producao = 1,
    Homologacao = 2
}
