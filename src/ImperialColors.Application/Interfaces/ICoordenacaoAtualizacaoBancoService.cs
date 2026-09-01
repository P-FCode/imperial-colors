using ImperialColors.Application.DTOs;

namespace ImperialColors.Application.Interfaces;

/// <summary>
/// Trava de coordenação entre PDVs: chamada uma vez a cada abertura do sistema, logo depois
/// de aplicar as migrations pendentes, para responder "algum outro caixa já atualizou o
/// banco compartilhado para uma versão que este código não conhece?".
///
/// Não é um lock — não impede duas instalações de abrirem ao mesmo tempo, nem serializa
/// nada. É um aviso: registra qual versão abriu por último e deixa quem está atrasado saber
/// disso antes de gravar qualquer dado, para o operador decidir se atualiza aquele caixa
/// antes de continuar vendendo.
/// </summary>
public interface ICoordenacaoAtualizacaoBancoService
{
    Task<ResultadoCoordenacaoBancoDto> VerificarERegistrarAsync(CancellationToken cancellationToken = default);
}
