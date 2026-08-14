using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace ImperialColors.Infrastructure.Security;

/// <summary>
/// Protege em repouso os três segredos fiscais guardados no banco — a API Key da API Fiscal
/// e o CSC de Homologação/Produção da NFC-e — usando a DPAPI do Windows com escopo de
/// MÁQUINA.
///
/// Por que: o CSC é o segredo que assina o QR Code da NFC-e; quem o obtém consegue emitir
/// cupom em nome da empresa. Em texto puro, esses valores saíam em qualquer <c>pg_dump</c>,
/// em qualquer backup do <c>BackupService</c> e em qualquer arquivo mandado para o suporte.
///
/// Escopo de MÁQUINA (não de usuário) porque o app pode ser aberto por contas Windows
/// diferentes no mesmo balcão, e um segredo cifrado pelo usuário A seria ilegível para o B.
///
/// ⚠️ Consequência conhecida e aceita: a chave da DPAPI pertence à máquina. Restaurar o banco
/// em OUTRO computador (troca de equipamento, recuperação de desastre) torna os três valores
/// ilegíveis — <see cref="Desproteger"/> devolve <c>null</c> nesse caso, o que faz as telas
/// mostrarem o campo vazio e as validações pedirem o cadastro de novo ("Cadastre a API Key da
/// API Fiscal em Configurações → Fiscal"). Os três são recuperáveis: a API Key sai do Portal
/// PFCode e o CSC do portal da SEFAZ do estado. Devolver <c>null</c> é deliberado — devolver o
/// texto cifrado faria o sistema mandar lixo para a API e falhar com um erro de autenticação
/// que não explicaria nada ao operador.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ProtecaoSegredoFiscal
{
    /// <summary>Marca o que já está cifrado. Sem ela não daria para distinguir um valor
    /// protegido de um valor legado em texto puro gravado antes desta proteção existir —
    /// e tentar decifrar texto puro lançaria exceção a cada leitura.</summary>
    private const string Prefixo = "dpapi:v1:";

    /// <summary>Entropia adicional: mesmo que outro processo na máquina chame a DPAPI, sem
    /// esta mesma entropia ele não decifra os valores do Imperial Colors.</summary>
    private static readonly byte[] Entropia = Encoding.UTF8.GetBytes("ImperialColors.SegredoFiscal.v1");

    public static string? Proteger(string? valorEmTextoPuro)
    {
        if (string.IsNullOrWhiteSpace(valorEmTextoPuro))
            return valorEmTextoPuro;

        // Idempotente: se já veio protegido (ex.: uma entidade relida e regravada sem que o
        // campo fosse tocado), não cifra duas vezes.
        if (valorEmTextoPuro.StartsWith(Prefixo, StringComparison.Ordinal))
            return valorEmTextoPuro;

        var cifrado = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(valorEmTextoPuro), Entropia, DataProtectionScope.LocalMachine);

        return Prefixo + Convert.ToBase64String(cifrado);
    }

    public static string? Desproteger(string? valorGravado)
    {
        if (string.IsNullOrWhiteSpace(valorGravado))
            return valorGravado;

        // Valor legado, gravado antes desta proteção existir: devolve como está. Na próxima
        // vez que a configuração for salva, ele passa a ser gravado cifrado.
        if (!valorGravado.StartsWith(Prefixo, StringComparison.Ordinal))
            return valorGravado;

        try
        {
            var cifrado = Convert.FromBase64String(valorGravado[Prefixo.Length..]);
            var puro = ProtectedData.Unprotect(cifrado, Entropia, DataProtectionScope.LocalMachine);
            return Encoding.UTF8.GetString(puro);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            // Banco restaurado em outra máquina (ou valor corrompido). Ver o aviso no sumário
            // da classe: null faz o sistema pedir o cadastro de novo, que é recuperável;
            // devolver o texto cifrado faria a API Fiscal responder 401 sem explicação.
            return null;
        }
    }
}
