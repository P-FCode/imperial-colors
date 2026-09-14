using ImperialColors.Application.Configuration;

namespace ImperialColors.UI.Services;

public interface IAppConfigService
{
    string ConnectionString { get; }

    string EmpresaNome { get; }
    string EmpresaSubtitulo { get; }
    string EmpresaRazaoSocial { get; }
    string EmpresaTelefone { get; }
    string EmpresaEmail { get; }
    string EmpresaEndereco { get; }
    string EmpresaCnpj { get; }

    EmpresaConfig Empresa { get; }

    string CupomRodape { get; }
    string BackupPath { get; }

    string IconPath { get; }
    string LogoPath { get; }
    string LogoSemFundoPath { get; }

    string ResolverCaminhoRecurso(string caminhoRelativo);

    /// <summary>
    /// Relê a configuração do processo depois que a tela de Configurações grava o .env, e
    /// avisa quem está exibindo esses dados através de <see cref="ConfiguracoesAlteradas"/>.
    /// </summary>
    void Recarregar();

    /// <summary>Disparado por <see cref="Recarregar"/>, na thread de quem chamou.</summary>
    event EventHandler? ConfiguracoesAlteradas;
}
