namespace ImperialColors.Application.Helpers;

public static class BackupScheduleHelper
{
    public const int IntervaloPadraoDias = 7;

    public static bool DeveExecutarBackup(DateTime? dataUltimoBackup, DateTime dataReferencia, int intervaloDias = IntervaloPadraoDias)
    {
        if (intervaloDias <= 0)
            intervaloDias = IntervaloPadraoDias;

        if (!dataUltimoBackup.HasValue)
            return true;

        return (dataReferencia.Date - dataUltimoBackup.Value.Date).Days >= intervaloDias;
    }
}
