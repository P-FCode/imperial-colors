namespace ImperialColors.Domain.Enums;

/// <summary>
/// Tipo de evento registrado no histórico de uma nota fiscal (<c>NotaFiscalEvento</c>) —
/// alimenta a janela "Ações da Nota" e espelha os eventos oficiais da seção 8 do
/// GUIA_INTEGRACAO.md (<c>110111</c>=Cancelamento, <c>110110</c>=CC-e).
/// </summary>
public enum TipoEventoNotaFiscal
{
    Emissao = 0,
    Cancelamento = 1,
    CartaCorrecao = 2,
    Inutilizacao = 3,
    ConsultaStatus = 4
}
