namespace ImperialColors.Domain.Enums;

/// <summary>
/// Regime tributário da empresa. Define se o cadastro de produto pede CSOSN
/// (Simples Nacional/MEI) ou CST (Regime Normal) para o ICMS — os dois campos são
/// mutuamente exclusivos por produto conforme o regime vigente da empresa.
///
/// Corresponde ao CRT (Código de Regime Tributário) exigido no XML da NF-e/NFC-e,
/// mas não é 1:1 — o CRT tem 4 valores oficiais (1=Simples Nacional, 2=Simples
/// excesso de sublimite, 3=Regime Normal, 4=MEI) e "excesso de sublimite" é um
/// sub-estado do Simples Nacional, não um regime próprio (ver
/// ConfiguracaoFiscalEmpresa.SimplesExcessoSublimite). LucroPresumido e LucroReal
/// mapeiam ambos para CRT 3 — o CRT não distingue os dois.
/// </summary>
public enum RegimeTributario
{
    SimplesNacional = 1,
    LucroPresumido = 2,
    LucroReal = 3,
    Mei = 4
}
