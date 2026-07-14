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
}
