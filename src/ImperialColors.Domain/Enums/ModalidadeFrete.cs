namespace ImperialColors.Domain.Enums;

/// <summary>Modalidade do frete (tag <c>transp.modFrete</c> da NF-e) — valores oficiais.</summary>
public enum ModalidadeFrete
{
    ContratacaoRemetenteCif = 0,
    ContratacaoDestinatarioFob = 1,
    ContratacaoTerceiros = 2,
    TransporteProprioRemetente = 3,
    TransporteProprioDestinatario = 4,
    SemOcorrenciaTransporte = 9
}
