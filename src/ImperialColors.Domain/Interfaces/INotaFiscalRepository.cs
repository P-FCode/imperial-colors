using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Domain.Interfaces;

public interface INotaFiscalRepository
{
    Task<NotaFiscal?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<NotaFiscal?> ObterPorChaveAcessoAsync(string chaveAcesso, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotaFiscal>> ListarAsync(TipoNotaFiscal tipo, StatusNotaFiscal? status = null, CancellationToken cancellationToken = default);

    /// <summary>Próximo número sequencial da série — a numeração é imutável (seção 10 do
    /// GUIA_INTEGRACAO.md): mesmo uma nota rejeitada consome o número, então a consulta
    /// ignora o filtro de soft-delete/status para nunca devolver um número já usado.
    ///
    /// Sequências de homologação e produção são independentes na SEFAZ (a chave de acesso
    /// nem codifica o ambiente — ver seção 8 do guia), então <paramref name="ambiente"/>
    /// faz parte da chave da numeração: uma nota de teste não pode consumir um número de
    /// produção.</summary>
    Task<string> ObterProximoNumeroAsync(TipoNotaFiscal tipo, string serie, AmbienteEmissaoFiscal ambiente, CancellationToken cancellationToken = default);

    /// <summary>Insere a nota com seus itens e pagamentos (rascunho novo).</summary>
    Task<NotaFiscal> CriarAsync(NotaFiscal nota, CancellationToken cancellationToken = default);

    /// <summary>Atualiza os campos escalares da nota (ex.: resultado da emissão). Quando
    /// <paramref name="substituirItens"/> é true, também substitui por completo as
    /// coleções de Itens/Pagamentos pelas contidas em <paramref name="nota"/> — só faz
    /// sentido enquanto a nota ainda está em Rascunho.</summary>
    Task AtualizarAsync(NotaFiscal nota, bool substituirItens = false, CancellationToken cancellationToken = default);

    Task<NotaFiscalEvento> AdicionarEventoAsync(NotaFiscalEvento evento, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete (Ativo=false) — a numeração permanece queimada mesmo depois
    /// (ver <see cref="ObterProximoNumeroAsync"/>, que ignora o filtro). A elegibilidade
    /// (só Rascunho/Rejeitada) é responsabilidade do Service, não deste repositório.</summary>
    Task ExcluirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Contadores para o painel-resumo da tela de Nota Fiscal — soma NF-e e NFC-e
    /// juntas (sem filtro por tipo). "Emitidas" conta só <see cref="StatusNotaFiscal.Autorizada"/>
    /// (uma rejeitada/rascunho não chegou a ser emitida de fato); o valor total soma
    /// apenas as Autorizadas.</summary>
    Task<(int Emitidas, int Canceladas, decimal ValorTotalEmitido)> ObterContadoresAsync(CancellationToken cancellationToken = default);

    /// <summary>Últimas N notas (NF-e + NFC-e juntas), ordenadas pela data de emissão mais
    /// recente — usado no painel-resumo da tela de Nota Fiscal.</summary>
    Task<IReadOnlyList<NotaFiscal>> ListarUltimasAsync(int quantidade, CancellationToken cancellationToken = default);
}
