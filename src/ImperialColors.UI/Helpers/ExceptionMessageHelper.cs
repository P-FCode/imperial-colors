using ImperialColors.Domain.Exceptions;
using ImperialColors.Infrastructure.Helpers;

namespace ImperialColors.UI.Helpers;

public static class ExceptionMessageHelper
{
    public static string ObterMensagemAmigavel(Exception exception)
    {
        if (exception is DomainException domain)
            return domain.Message;

        var detalhe = DatabaseExceptionHelper.ObterMensagemDetalhada(exception);
        if (!string.IsNullOrWhiteSpace(detalhe) &&
            !detalhe.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase))
        {
            return detalhe.StartsWith("Erro real do banco:", StringComparison.OrdinalIgnoreCase)
                ? detalhe
                : $"Erro real do banco: {detalhe}";
        }

        return exception.Message;
    }
}
