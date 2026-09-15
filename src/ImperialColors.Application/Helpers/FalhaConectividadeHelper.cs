using System.Data.Common;
using System.Net.Sockets;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Decide se uma exceção significa "o banco está inacessível" (vale entrar em contingência offline)
/// ou "o banco respondeu recusando o dado" (a venda precisa falhar com o erro visível).
/// </summary>
public static class FalhaConectividadeHelper
{
    // Classes/códigos SQLSTATE em que o servidor está de pé mas não pode atender:
    // 08 conexão, 28 autenticação, 3D000 banco inexistente, 53 recursos, 57P desligamento/recuperação.
    private static readonly string[] PrefixosIndisponibilidade = ["08", "28", "3D000", "53", "57P"];

    public static bool EhFalhaDeConectividade(Exception? excecao)
    {
        for (var atual = excecao; atual is not null; atual = atual.InnerException)
        {
            if (atual is DbException db)
            {
                // Sem SQLSTATE a falha aconteceu antes de o servidor responder (rede, socket, timeout).
                if (string.IsNullOrEmpty(db.SqlState))
                    return true;

                return PrefixosIndisponibilidade.Any(p => db.SqlState.StartsWith(p, StringComparison.Ordinal));
            }

            if (atual is TimeoutException or IOException or SocketException)
                return true;
        }

        return false;
    }
}
