using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Helpers;
using ImperialColors.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImperialColors.Infrastructure.Atualizacao;

/// <summary>
/// Implementação da trava de coordenação entre PDVs, sobre a tabela de parâmetros do sistema
/// que já existia (<see cref="ParametroSistemaChaves.VersaoBancoAplicada"/>).
///
/// Não usa transação nem advisory lock: o pior cenário de duas instalações lendo e gravando
/// ao mesmo tempo é uma delas perder uma atualização do registro por uma fração de segundo,
/// o que se autocorrige na próxima abertura de qualquer uma delas. Isto é um aviso para o
/// operador agir, não uma garantia de integridade transacional — essa garantia continua
/// sendo o índice único e o soft-delete que já protegem os dados de negócio.
/// </summary>
public sealed class CoordenacaoAtualizacaoBancoService : ICoordenacaoAtualizacaoBancoService
{
    private readonly IParametroSistemaRepository _parametros;
    private readonly ILogger<CoordenacaoAtualizacaoBancoService> _logger;

    public CoordenacaoAtualizacaoBancoService(
        IParametroSistemaRepository parametros,
        ILogger<CoordenacaoAtualizacaoBancoService> logger)
    {
        _parametros = parametros;
        _logger = logger;
    }

    public async Task<ResultadoCoordenacaoBancoDto> VerificarERegistrarAsync(CancellationToken cancellationToken = default)
    {
        var versaoInstalada = AtualizadorSistemaService.ObterVersaoInstalada();

        var bruto = await _parametros.ObterTextoAsync(ParametroSistemaChaves.VersaoBancoAplicada, cancellationToken);

        Version? versaoRegistrada = null;
        string? maquinaRegistrada = null;

        if (RegistroVersaoBancoHelper.TentarConverter(bruto, out var versaoLida, out var maquinaLida))
        {
            versaoRegistrada = versaoLida;
            maquinaRegistrada = maquinaLida;
        }
        else if (!string.IsNullOrWhiteSpace(bruto))
        {
            // Formato inesperado (edição manual do parâmetro, versão futura do formato) —
            // trata como "sem registro" em vez de travar a abertura do sistema por causa de
            // um dado auxiliar corrompido. A ausência de coordenação nunca pode ser pior do
            // que a coordenação que ela substitui.
            _logger.LogWarning(
                "Parâmetro {Chave} com valor em formato inesperado: '{Bruto}'. Tratando como se não houvesse registro.",
                ParametroSistemaChaves.VersaoBancoAplicada, bruto);
        }

        var atrasado = versaoRegistrada is not null && versaoRegistrada > versaoInstalada;

        // Só regrava quando esta instalação é estritamente mais nova que o registro (ou não
        // existe registro ainda): uma instalação atrasada nunca pode rebaixar o registro, e
        // uma instalação empatada não precisa reescrever nada a cada abertura — o registro
        // existe para marcar QUEM AVANÇOU a versão do banco, não para virar um log de acessos.
        if (!atrasado && (versaoRegistrada is null || versaoInstalada > versaoRegistrada))
        {
            var registro = RegistroVersaoBancoHelper.Formatar(versaoInstalada, Environment.MachineName);
            await _parametros.SalvarTextoAsync(ParametroSistemaChaves.VersaoBancoAplicada, registro, cancellationToken);
        }

        return new ResultadoCoordenacaoBancoDto
        {
            BancoAtualizadoPorOutraInstalacaoMaisNova = atrasado,
            VersaoInstalada = versaoInstalada,
            VersaoRegistradaNoBanco = versaoRegistrada,
            MaquinaQueAtualizouPorUltimo = maquinaRegistrada
        };
    }
}
