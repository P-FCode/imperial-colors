namespace ImperialColors.Application.Configuration;

/// <summary>
/// Permite sobrescrever dados da empresa via .env sem recompilar (prioridade sobre appsettings.json).
/// </summary>
public static class EmpresaConfigEnvironmentOverrides
{
    public static void Aplicar(EmpresaConfig config)
    {
        AplicarSeDefinido("EMPRESA_NOME", v => config.NomeFantasia = v);
        AplicarSeDefinido("EMPRESA_RAZAO_SOCIAL", v => config.RazaoSocial = v);
        AplicarSeDefinido("EMPRESA_SUBTITULO", v => config.Subtitulo = v);
        AplicarSeDefinido("EMPRESA_CNPJ", v => config.CNPJ = v);
        AplicarSeDefinido("EMPRESA_IE", v => config.InscricaoEstadual = v);
        AplicarSeDefinido("EMPRESA_ENDERECO", v => config.Endereco = v);
        AplicarSeDefinido("EMPRESA_TELEFONE", v => config.Telefone = v);
    }

    private static void AplicarSeDefinido(string chave, Action<string> aplicar)
    {
        var valor = Environment.GetEnvironmentVariable(chave)?.Trim();
        if (!string.IsNullOrEmpty(valor))
            aplicar(valor);
    }
}
