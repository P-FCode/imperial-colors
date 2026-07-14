using Microsoft.EntityFrameworkCore;

namespace ImperialColors.Infrastructure.Helpers;

public static class DatabaseExceptionHelper
{
    public static string ObterMensagemDetalhada(Exception exception)
    {
        if (exception is DbUpdateException dbUpdate)
            return ExtrairMensagem(dbUpdate);

        if (exception.InnerException is DbUpdateException innerDb)
            return ExtrairMensagem(innerDb);

        return exception.InnerException?.Message ?? exception.Message;
    }

    private static string ExtrairMensagem(DbUpdateException exception)
    {
        var mensagens = new List<string> { exception.Message };

        var atual = exception.InnerException;
        while (atual is not null)
        {
            if (!string.IsNullOrWhiteSpace(atual.Message) && !mensagens.Contains(atual.Message))
                mensagens.Add(atual.Message);
            atual = atual.InnerException;
        }

        return string.Join("\n\n", mensagens);
    }

    public static bool EhViolacaoUnicidadeCodigoInterno(Exception exception)
    {
        var detalhe = ObterMensagemDetalhada(exception);
        return detalhe.Contains("IX_produtos_codigo_interno", StringComparison.OrdinalIgnoreCase)
               || (detalhe.Contains("23505", StringComparison.OrdinalIgnoreCase)
                   && detalhe.Contains("codigo_interno", StringComparison.OrdinalIgnoreCase));
    }
}
