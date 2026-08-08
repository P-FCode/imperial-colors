namespace ImperialColors.Domain.Constants;

public static class ParametroSistemaChaves
{
    public const string DataUltimoBackup = "DataUltimoBackup";

    /// <summary>
    /// Regime tributário da empresa (valor = nome do enum RegimeTributario).
    /// Decide se o cadastro de produto pede CSOSN (Simples Nacional) ou CST
    /// (Lucro Presumido/Real) para o ICMS.
    /// </summary>
    public const string RegimeTributarioEmpresa = "RegimeTributarioEmpresa";
}
