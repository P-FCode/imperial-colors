namespace ImperialColors.Domain.Helpers;

/// <summary>
/// Formata e interpreta o registro "qual instalação abriu por último o banco compartilhado,
/// em que versão" — a peça central da trava de coordenação entre PDVs.
///
/// O problema que ela resolve: o banco PostgreSQL é um só, compartilhado por todos os
/// caixas, mas cada caixa roda sua própria cópia dos arquivos. Quando um caixa atualiza e
/// abre primeiro, ele aplica as migrations pendentes NO BANCO COMPARTILHADO — na hora, antes
/// de qualquer outro caixa saber que existe uma versão nova. Um caixa ainda na versão antiga,
/// se aberto nesse intervalo, está rodando um modelo do EF Core que não conhece a estrutura
/// atual da tabela. Se a mudança foi aditiva (coluna nova, índice novo) isso é inofensivo —
/// o código antigo simplesmente ignora o que não conhece. Se foi uma mudança incompatível
/// (coluna renomeada, tipo trocado, NOT NULL sem valor padrão), a próxima operação desse
/// caixa antigo falha na hora, no meio de uma venda.
///
/// Este registro é gravado em <c>ParametroSistemaChaves.VersaoBancoAplicada</c> (a tabela de
/// parâmetros key-value já existente) por qualquer instalação cuja versão seja maior ou
/// igual à registrada, no momento em que ela abre. Uma instalação mais antiga que encontra um
/// registro mais novo sabe, antes de fazer qualquer operação, que está desatualizada em
/// relação ao banco.
/// </summary>
public static class RegistroVersaoBancoHelper
{
    private const char Separador = '@';

    /// <summary>
    /// Formata "1.3.0@DESKTOP-CAIXA2". O nome da máquina não precisa de escape: nomes de
    /// máquina Windows não contêm '@', e mesmo que contivessem, <see cref="TentarConverter"/>
    /// separa só na primeira ocorrência — o resto vira parte do nome da máquina.
    /// </summary>
    public static string Formatar(Version versao, string maquina) =>
        $"{VersaoRelease.Formatar(versao)}{Separador}{maquina}";

    /// <summary>
    /// Devolve <c>false</c> para qualquer valor que não tenha essa forma — incluindo um
    /// registro vazio (banco novo, ninguém abriu ainda) e um valor corrompido ou de um
    /// formato futuro incompatível. Em nenhum desses casos o chamador deve travar a
    /// abertura do sistema: a ausência de coordenação nunca pode ser mais perigosa do que a
    /// coordenação que ela substitui.
    /// </summary>
    public static bool TentarConverter(string? valor, out Version versao, out string maquina)
    {
        versao = new Version(0, 0, 0);
        maquina = string.Empty;

        if (string.IsNullOrWhiteSpace(valor))
            return false;

        var partes = valor.Split(Separador, 2);
        if (partes.Length != 2)
            return false;

        if (!VersaoRelease.TentarConverter(partes[0], out versao))
            return false;

        maquina = partes[1];
        return true;
    }
}
