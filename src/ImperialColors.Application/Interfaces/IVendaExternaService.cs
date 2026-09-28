using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;

namespace ImperialColors.Application.Interfaces;

public interface IVendaExternaService
{
    Task<IEnumerable<VendaExternaDto>> ObterTodosAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<VendaExternaDto>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);

    Task<PaginacaoResultadoDto<VendaExternaDto>> ObterPaginadoAsync(
        int pagina, int itensPorPagina, string? termoBusca = null, CancellationToken cancellationToken = default);
    Task<VendaExternaDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<VendaExternaDto> RegistrarAsync(RegistrarVendaExternaDto dto, CancellationToken cancellationToken = default);
    Task<VendaExternaDto> AtualizarAsync(AtualizarVendaExternaDto dto, CancellationToken cancellationToken = default);
    Task ExcluirFisicamenteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Importa a lista de conferência a partir de um arquivo de texto (TXT ou CSV):
    /// interpreta as linhas e vincula ao estoque o que tiver código de barras cadastrado.</summary>
    Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ProcessarImportacaoTextoAsync(
        string conteudoArquivo, FormatoImportacaoLista formato, CancellationToken cancellationToken = default);

    /// <summary>Vendas externas com comissão, para a tela de controle de acertos. Venda sem
    /// comissão não aparece: não há o que pagar nela.</summary>
    Task<IReadOnlyList<ComissaoVendaExternaDto>> ListarComissoesAsync(
        FiltroComissaoVendaExterna filtro, CancellationToken cancellationToken = default);

    /// <summary>Totais de comissão (a pagar, pago, do mês) e a lista de pendentes — usado
    /// pelo painel de Comissões do Dashboard e pela tela de controle.</summary>
    Task<ResumoComissoesDto> ObterResumoComissoesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marca ou desmarca o acerto da comissão de uma venda externa.</summary>
    Task MarcarComissaoAsync(int vendaExternaId, bool paga, CancellationToken cancellationToken = default);

    /// <summary>Mesmo que <see cref="ProcessarImportacaoTextoAsync"/>, mas para planilha:
    /// recebe as células já lidas do .xlsx pela tela — a leitura do arquivo do Excel mora na
    /// camada de apresentação, junto com a geração de relatórios.</summary>
    Task<IReadOnlyList<LinhaImportacaoVendaExternaDto>> ProcessarImportacaoPlanilhaAsync(
        IReadOnlyList<LinhaBrutaImportacaoDto> celulas, CancellationToken cancellationToken = default);
}
