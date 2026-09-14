using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;
using System.IO;

namespace ImperialColors.UI.Services;

/// <summary>Dados cadastrais da empresa editáveis na tela de Configurações.</summary>
public record ConfiguracoesEmpresaInput(
    string NomeFantasia,
    string RazaoSocial,
    string Subtitulo,
    string Cnpj,
    string InscricaoEstadual,
    string Endereco,
    string Telefone,
    string Email);

/// <summary>Preferências gerais que hoje só existiam no .env.</summary>
public record ConfiguracoesGeraisInput(string CupomRodape, string PastaBackup);

public interface IConfiguracoesAplicacaoService
{
    string CaminhoArquivoEnv { get; }

    ConfiguracoesEmpresaInput ObterEmpresaAtual();
    ConfiguracoesGeraisInput ObterGeraisAtual();

    Task SalvarEmpresaAsync(ConfiguracoesEmpresaInput input, CancellationToken cancellationToken = default);
    Task SalvarGeraisAsync(ConfiguracoesGeraisInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Edição das configurações que moram no .env, feita de dentro do sistema.
///
/// O fluxo é sempre: validar → gravar o .env → recarregar a configuração do processo →
/// registrar na auditoria. Gravar antes de validar deixaria o cliente com um .env quebrado,
/// e recarregar antes de gravar faria a tela mostrar um valor que não sobreviveria ao
/// próximo reinício.
///
/// A conexão com o banco fica de fora de propósito: trocar servidor/porta/base é tarefa de
/// instalação, feita direto no .env, e um erro ali deixa o sistema sem abrir — sem tela para
/// corrigir. Em Configurações ela aparece somente leitura, com a senha mascarada.
/// </summary>
public class ConfiguracoesAplicacaoService : IConfiguracoesAplicacaoService
{
    private const string ModuloAuditoria = "Configurações";

    private readonly IArquivoEnvService _arquivoEnv;
    private readonly IAppConfigService _config;
    private readonly IAuditoriaService _auditoria;
    private readonly ISessaoService _sessao;

    public ConfiguracoesAplicacaoService(
        IArquivoEnvService arquivoEnv,
        IAppConfigService config,
        IAuditoriaService auditoria,
        ISessaoService sessao)
    {
        _arquivoEnv = arquivoEnv;
        _config = config;
        _auditoria = auditoria;
        _sessao = sessao;
    }

    public string CaminhoArquivoEnv => _arquivoEnv.Caminho;

    public ConfiguracoesEmpresaInput ObterEmpresaAtual()
    {
        var empresa = _config.Empresa;

        return new ConfiguracoesEmpresaInput(
            empresa.NomeFantasia,
            empresa.RazaoSocial,
            empresa.Subtitulo,
            empresa.CNPJ,
            empresa.InscricaoEstadual,
            empresa.Endereco,
            empresa.Telefone,
            empresa.Email);
    }

    public ConfiguracoesGeraisInput ObterGeraisAtual()
        => new(_config.CupomRodape, _config.BackupPath);

    public async Task SalvarEmpresaAsync(
        ConfiguracoesEmpresaInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidarEmpresa(input);

        await _arquivoEnv.SalvarAsync(new Dictionary<string, string>
        {
            ["EMPRESA_NOME"] = input.NomeFantasia.Trim(),
            ["EMPRESA_RAZAO_SOCIAL"] = input.RazaoSocial.Trim(),
            ["EMPRESA_SUBTITULO"] = input.Subtitulo.Trim(),
            ["EMPRESA_CNPJ"] = input.Cnpj.Trim(),
            ["EMPRESA_IE"] = input.InscricaoEstadual.Trim(),
            ["EMPRESA_ENDERECO"] = input.Endereco.Trim(),
            ["EMPRESA_TELEFONE"] = input.Telefone.Trim(),
            ["EMPRESA_EMAIL"] = input.Email.Trim()
        }, cancellationToken);

        _config.Recarregar();

        await RegistrarAuditoriaAsync(
            "CONFIGURACAO_EMPRESA_ALTERADA",
            $"Cadastro da empresa atualizado para '{input.NomeFantasia.Trim()}'.",
            cancellationToken);
    }

    public async Task SalvarGeraisAsync(
        ConfiguracoesGeraisInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidarGerais(input);

        await _arquivoEnv.SalvarAsync(new Dictionary<string, string>
        {
            ["CUPOM_MENSAGEM_RODAPE"] = input.CupomRodape.Trim(),
            ["BACKUP_PATH"] = input.PastaBackup.Trim()
        }, cancellationToken);

        _config.Recarregar();

        await RegistrarAuditoriaAsync(
            "CONFIGURACAO_GERAL_ALTERADA",
            $"Rodapé do cupom e pasta de backup atualizados (backup em '{input.PastaBackup.Trim()}').",
            cancellationToken);
    }

    private static void ValidarEmpresa(ConfiguracoesEmpresaInput input)
    {
        if (string.IsNullOrWhiteSpace(input.NomeFantasia))
            throw new DomainException("Informe o nome fantasia da empresa.");

        if (string.IsNullOrWhiteSpace(input.RazaoSocial))
            throw new DomainException("Informe a razão social da empresa.");

        // CNPJ é opcional (a loja pode operar só com cupom não fiscal), mas se vier
        // preenchido precisa ser válido — um CNPJ errado só apareceria na rejeição da nota.
        if (!string.IsNullOrWhiteSpace(input.Cnpj) && !DocumentoFiscalHelper.CnpjValido(input.Cnpj))
            throw new DomainException("CNPJ inválido — confira os dígitos digitados.");

        if (!string.IsNullOrWhiteSpace(input.Email) && !input.Email.Contains('@'))
            throw new DomainException("E-mail inválido.");
    }

    private static void ValidarGerais(ConfiguracoesGeraisInput input)
    {
        if (string.IsNullOrWhiteSpace(input.PastaBackup))
            throw new DomainException("Informe a pasta onde os backups devem ser gravados.");

        if (input.PastaBackup.Intersect(Path.GetInvalidPathChars()).Any())
            throw new DomainException("A pasta de backup contém caracteres inválidos.");
    }

    private Task RegistrarAuditoriaAsync(
        string acao,
        string descricao,
        CancellationToken cancellationToken,
        NivelLogAuditoria nivel = NivelLogAuditoria.Info)
        => _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = _sessao.UsuarioAtual?.NomeCompleto ?? _sessao.ObterNomeUsuario(),
            Modulo = ModuloAuditoria,
            Acao = acao,
            Descricao = descricao,
            Nivel = nivel
        }, cancellationToken);
}
