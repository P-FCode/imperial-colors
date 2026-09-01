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

    /// <summary>
    /// Versão e máquina da última instalação que abriu este banco compartilhado (valor no
    /// formato de <see cref="ImperialColors.Domain.Helpers.RegistroVersaoBancoHelper"/>, ex.:
    /// "1.3.0@DESKTOP-CAIXA2").
    /// É a trava de coordenação entre PDVs: sem ela, um caixa que atualiza e migra o schema
    /// não tem como avisar os outros caixas — ainda na versão antiga — de que a estrutura do
    /// banco compartilhado mudou.
    /// </summary>
    public const string VersaoBancoAplicada = "VersaoBancoAplicada";
}
