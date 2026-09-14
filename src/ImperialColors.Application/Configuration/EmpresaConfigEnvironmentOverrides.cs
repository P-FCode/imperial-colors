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
        AplicarSeDefinido("EMPRESA_EMAIL", v => config.Email = v);
    }

    /// <summary>
    /// Variável ausente significa "não opino, use o appsettings.json"; variável presente manda
    /// no valor mesmo quando vazia. A distinção importa desde que a tela de Configurações passou
    /// a gravar o .env: sem ela, limpar a Inscrição Estadual na tela não teria efeito nenhum —
    /// o campo voltaria com o valor de exemplo do appsettings.json na próxima abertura.
    /// </summary>
    private static void AplicarSeDefinido(string chave, Action<string> aplicar)
    {
        if (Environment.GetEnvironmentVariable(chave) is not { } valor)
            return;

        aplicar(valor.Trim());
    }
}
