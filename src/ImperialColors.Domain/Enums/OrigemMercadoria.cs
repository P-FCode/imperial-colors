namespace ImperialColors.Domain.Enums;

/// <summary>
/// Tabela oficial de Origem da Mercadoria (usada no ICMS de NF-e/NFC-e).
/// Valores e descrições conforme o Manual de Orientação do Contribuinte (SEFAZ).
/// </summary>
public enum OrigemMercadoria
{
    Nacional = 0,
    EstrangeiraImportacaoDireta = 1,
    EstrangeiraMercadoInterno = 2,
    NacionalImportacaoSuperior40Porcento = 3,
    NacionalProcessoProdutivoBasico = 4,
    NacionalImportacaoAteh40Porcento = 5,
    EstrangeiraImportacaoDiretaSemSimilarNacional = 6,
    EstrangeiraMercadoInternoSemSimilarNacional = 7,
    NacionalImportacaoSuperior70Porcento = 8
}
