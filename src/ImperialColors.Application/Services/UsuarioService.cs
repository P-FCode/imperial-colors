using ImperialColors.Domain.Helpers;
using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Application.Services;

public class UsuarioService : IUsuarioService
{
    private const string ModuloAuditoria = "Usuários";

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAuditoriaService _auditoria;
    private readonly IUsuarioAtual _usuarioAtual;
    private readonly ILogger<UsuarioService> _logger;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IAuditoriaService auditoria,
        IUsuarioAtual usuarioAtual,
        ILogger<UsuarioService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _auditoria = auditoria;
        _usuarioAtual = usuarioAtual;
        _logger = logger;
    }

    public async Task<IEnumerable<UsuarioDto>> ObterTodosAsync()
    {
        var usuarios = await _usuarioRepository.ObterTodosAsync();
        return usuarios.Select(MapParaDto);
    }

    public async Task<UsuarioDto?> ObterPorIdAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id);
        return usuario is null ? null : MapParaDto(usuario);
    }

    public async Task<UsuarioDto> CriarPorAdminAsync(CriarUsuarioAdminDto dto)
    {
        var nome = InputSanitizer.SanitizarTexto(dto.NomeCompleto, 200);
        var username = InputSanitizer.SanitizarUsername(dto.Username);
        var email = InputSanitizer.SanitizarEmail(dto.Email);

        ValidarDadosBasicos(nome, username, email, dto.Senha);

        if (await _usuarioRepository.UsernameExisteAsync(username))
            throw new DomainException("Nome de usuário já está em uso.");
        if (await _usuarioRepository.EmailExisteAsync(email))
            throw new DomainException("E-mail já está cadastrado.");

        var (hash, salt) = PasswordHasher.HashPassword(dto.Senha);

        var usuario = new Usuario
        {
            NomeCompleto = nome,
            Username = username,
            Email = email,
            SenhaHash = hash,
            Salt = salt,
            Status = StatusUsuario.Aprovado,
            Permissao = dto.Permissao,
            DataCadastro = Relogio.Agora
        };

        var criado = await _usuarioRepository.AdicionarAsync(usuario);
        _logger.LogInformation("Usuário criado pelo admin: {Username} ({Permissao})", username, dto.Permissao);

        await RegistrarAuditoriaAsync("USUARIO_CRIADO",
            $"Usuário '{criado.Username}' criado com permissão {criado.Permissao}", criado);

        return MapParaDto(criado);
    }

    public async Task<UsuarioDto> AtualizarPorAdminAsync(AtualizarUsuarioAdminDto dto)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(dto.Id)
            ?? throw new DomainException("Usuário não encontrado.");

        var nome = InputSanitizer.SanitizarTexto(dto.NomeCompleto, 200);
        var email = InputSanitizer.SanitizarEmail(dto.Email);

        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome completo é obrigatório.");
        if (!InputSanitizer.EmailValido(email))
            throw new DomainException("E-mail inválido.");

        if (await _usuarioRepository.EmailExisteAsync(email, dto.Id))
            throw new DomainException("E-mail já está em uso por outro usuário.");

        await GarantirQueSobraAdministradorAsync(usuario,
            continuaAdministradorAtivo: dto.Permissao == PermissaoUsuario.Admin && dto.Status == StatusUsuario.Aprovado,
            "Não é possível tirar a permissão de administrador do único administrador aprovado do sistema. " +
            "Cadastre outro administrador antes.");

        var mudancas = new List<string>();
        if (usuario.Permissao != dto.Permissao) mudancas.Add($"permissão {usuario.Permissao} → {dto.Permissao}");
        if (usuario.Status != dto.Status) mudancas.Add($"situação {usuario.Status} → {dto.Status}");
        if (usuario.NomeCompleto != nome) mudancas.Add("nome alterado");
        if (usuario.Email != email) mudancas.Add("e-mail alterado");

        usuario.NomeCompleto = nome;
        usuario.Email = email;
        usuario.Permissao = dto.Permissao;
        usuario.Status = dto.Status;

        if (!string.IsNullOrWhiteSpace(dto.NovaSenha))
        {
            if (!InputSanitizer.SenhaForte(dto.NovaSenha))
                throw new DomainException("Nova senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número.");

            var (hash, salt) = PasswordHasher.HashPassword(dto.NovaSenha);
            usuario.SenhaHash = hash;
            usuario.Salt = salt;
            mudancas.Add("senha redefinida");
        }

        var atualizado = await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário atualizado: {Username}", usuario.Username);

        if (mudancas.Count > 0)
            await RegistrarAuditoriaAsync("USUARIO_ALTERADO",
                $"Usuário '{usuario.Username}' alterado: {string.Join("; ", mudancas)}", usuario, NivelLogAuditoria.Warning);

        return MapParaDto(atualizado);
    }

    public async Task CancelarAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        await GarantirQueSobraAdministradorAsync(usuario, continuaAdministradorAtivo: false,
            "Não é possível cancelar o único administrador aprovado do sistema. " +
            "Cadastre outro administrador antes.");

        usuario.Status = StatusUsuario.Cancelado;
        await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário cancelado: {Username}", usuario.Username);

        await RegistrarAuditoriaAsync("USUARIO_CANCELADO",
            $"Usuário '{usuario.Username}' cancelado", usuario, NivelLogAuditoria.Warning);
    }

    public async Task AprovarAsync(Guid id)
    {
        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        usuario.Status = StatusUsuario.Aprovado;
        await _usuarioRepository.AtualizarAsync(usuario);
        _logger.LogInformation("Usuário aprovado: {Username}", usuario.Username);

        await RegistrarAuditoriaAsync("USUARIO_APROVADO",
            $"Usuário '{usuario.Username}' aprovado com permissão {usuario.Permissao}", usuario);
    }

    public async Task ExcluirFisicamenteAsync(Guid id, Guid usuarioLogadoId)
    {
        if (id == usuarioLogadoId)
            throw new DomainException("Você não pode excluir o usuário da sessão ativa.");

        var usuario = await _usuarioRepository.ObterPorIdAsync(id)
            ?? throw new DomainException("Usuário não encontrado.");

        await GarantirQueSobraAdministradorAsync(usuario, continuaAdministradorAtivo: false,
            "Não é possível excluir o único administrador aprovado do sistema. Cadastre outro administrador antes.");

        await _usuarioRepository.RemoverFisicamenteAsync(id);
        _logger.LogInformation("Usuário excluído permanentemente: {Username}", usuario.Username);

        await RegistrarAuditoriaAsync("USUARIO_EXCLUIDO",
            $"Usuário '{usuario.Username}' ({usuario.NomeCompleto}) excluído permanentemente", usuario, NivelLogAuditoria.Warning);
    }

    /// <summary>
    /// Impede o lockout da loja: quem já é administrador aprovado só pode deixar de ser — por
    /// rebaixamento de permissão, mudança de situação, cancelamento ou exclusão — se houver
    /// outro administrador aprovado. Sem isso ninguém mais abre a Gestão de Usuários e não
    /// sobra tela para voltar atrás.
    /// </summary>
    private async Task GarantirQueSobraAdministradorAsync(
        Usuario usuario, bool continuaAdministradorAtivo, string mensagem)
    {
        var eraAdministradorAtivo =
            usuario.Permissao == PermissaoUsuario.Admin && usuario.Status == StatusUsuario.Aprovado;

        if (!eraAdministradorAtivo || continuaAdministradorAtivo)
            return;

        // O próprio usuário ainda entra na contagem: 1 significa que ele é o último.
        if (await _usuarioRepository.ContarAdminsAprovadosAsync() <= 1)
            throw new DomainException(mensagem);
    }

    private static void ValidarDadosBasicos(string nome, string username, string email, string senha)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("Nome completo é obrigatório.");
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            throw new DomainException("Nome de usuário deve ter pelo menos 3 caracteres.");
        if (!InputSanitizer.EmailValido(email))
            throw new DomainException("E-mail inválido.");
        if (!InputSanitizer.SenhaForte(senha))
            throw new DomainException("Senha deve ter no mínimo 8 caracteres, incluindo maiúscula, minúscula e número.");
    }

    private Task RegistrarAuditoriaAsync(
        string acao, string descricao, Usuario alvo, NivelLogAuditoria nivel = NivelLogAuditoria.Info)
        => _auditoria.RegistrarAsync(new RegistrarLogAuditoriaDto
        {
            NomeUsuario = _usuarioAtual.Nome,
            Modulo = ModuloAuditoria,
            Acao = acao,
            Descricao = descricao,
            Nivel = nivel,
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                alvo.Id, alvo.Username, alvo.NomeCompleto, alvo.Email,
                Permissao = alvo.Permissao.ToString(), Status = alvo.Status.ToString()
            })
        });

    private static UsuarioDto MapParaDto(Usuario u) => new()
    {
        Id = u.Id,
        NomeCompleto = u.NomeCompleto,
        Username = u.Username,
        Email = u.Email,
        Status = u.Status,
        Permissao = u.Permissao,
        DataCadastro = u.DataCadastro
    };
}
