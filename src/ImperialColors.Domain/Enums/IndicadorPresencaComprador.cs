namespace ImperialColors.Domain.Enums;

/// <summary>Indicador de presença do comprador (tag <c>ide.indPres</c> da NF-e) — valores oficiais.</summary>
public enum IndicadorPresencaComprador
{
    NaoSeAplica = 0,
    Presencial = 1,
    NaoPresencialInternet = 2,
    NaoPresencialTeleatendimento = 3,
    NfcePresencialEntregaDomicilio = 4,
    PresencialForaDoEstabelecimento = 5,
    NaoPresencialOutros = 9
}
