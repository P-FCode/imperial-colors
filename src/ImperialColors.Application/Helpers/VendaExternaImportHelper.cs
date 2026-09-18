using System.Globalization;
using System.Text;
using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Helpers;

/// <summary>
/// Lê a lista de conferência de uma venda externa nos formatos aceitos. As colunas são
/// sempre as mesmas — código de barras, nome do produto e quantidade —; o que muda é só
/// como elas chegam até aqui.
///
/// TXT e CSV são texto e o próprio helper separa os campos. Planilha não: quem abre o
/// .xlsx é a camada de apresentação (é lá que mora o ClosedXML, junto da geração de
/// relatórios) e entrega as células já convertidas em texto por
/// <see cref="ParseCelulas"/> — assim as regras de validação, mensagem de erro e
/// numeração de linha ficam num lugar só, iguais para os três formatos.
/// </summary>
public static class VendaExternaImportHelper
{
    public const string FormatoEsperado = "CODIGO_DE_BARRAS;NOME_DO_PRODUTO;QUANTIDADE";

    /// <summary>Formatos que chegam como texto — TXT e CSV.</summary>
    public static IReadOnlyList<LinhaImportacaoVendaExternaDto> ParseTexto(
        string conteudo, FormatoImportacaoLista formato = FormatoImportacaoLista.Txt)
    {
        if (formato == FormatoImportacaoLista.Xlsx)
            throw new DomainException("Planilha .xlsx não é texto — use ParseCelulas com as células já lidas.");

        var rotulo = formato == FormatoImportacaoLista.Csv ? "CSV" : "TXT";

        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException($"O arquivo {rotulo} está vazio.");

        var linhas = conteudo.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (linhas.Length == 0)
            throw new DomainException($"Nenhuma linha válida encontrada no arquivo {rotulo}.");

        // O separador do CSV é detectado na primeira linha com conteúdo: o Excel em
        // português salva com ponto e vírgula, mas arquivo vindo de outro sistema (ou de
        // uma planilha do Google) costuma vir com vírgula. O TXT continua fixo em ponto e
        // vírgula — é o formato que o módulo sempre aceitou, e mudar isso reinterpretaria
        // arquivos antigos.
        var separador = formato == FormatoImportacaoLista.Csv ? DetectarSeparador(linhas) : ';';

        var brutas = new List<LinhaBrutaImportacaoDto>();

        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith('#'))
                continue;

            var campos = formato == FormatoImportacaoLista.Csv
                ? SepararCsv(linha, separador)
                : linha.Split(separador).ToList();

            if (campos.Count < 3)
                throw new DomainException($"Linha {i + 1}: formato inválido. Use {FormatoEsperado}.");

            brutas.Add(new LinhaBrutaImportacaoDto
            {
                NumeroLinha = i + 1,
                CodigoBarras = campos[0],
                NomeProduto = campos[1],
                Quantidade = campos[2]
            });
        }

        return Montar(brutas, rotulo);
    }

    /// <summary>Células de planilha já lidas pela tela, uma entrada por linha da aba.</summary>
    public static IReadOnlyList<LinhaImportacaoVendaExternaDto> ParseCelulas(IReadOnlyList<LinhaBrutaImportacaoDto> celulas)
    {
        if (celulas.Count == 0)
            throw new DomainException("A planilha está vazia.");

        return Montar(celulas, "XLSX");
    }

    private static IReadOnlyList<LinhaImportacaoVendaExternaDto> Montar(
        IReadOnlyList<LinhaBrutaImportacaoDto> brutas, string rotuloFormato)
    {
        var resultado = new List<LinhaImportacaoVendaExternaDto>();
        var primeiraLinhaComConteudo = true;

        foreach (var bruta in brutas)
        {
            var codigo = (bruta.CodigoBarras ?? string.Empty).Trim();
            var nome = (bruta.NomeProduto ?? string.Empty).Trim();
            var textoQuantidade = (bruta.Quantidade ?? string.Empty).Trim();

            if (codigo.Length == 0 && nome.Length == 0 && textoQuantidade.Length == 0)
                continue;

            if (codigo.StartsWith('#'))
                continue;

            var quantidadeValida = TentarLerQuantidade(textoQuantidade, out var quantidade);

            // Cabeçalho: tanto o Excel quanto o "salvar como CSV" trazem a linha de títulos
            // junto, e não faz sentido exigir que o operador a apague à mão antes de
            // importar. Só a PRIMEIRA linha com conteúdo ganha esse perdão, e só quando a
            // quantidade traz texto que não é número ("Quantidade", "Qtd") — quantidade
            // VAZIA continua sendo erro, senão uma linha truncada de verdade passaria
            // despercebida logo na primeira posição do arquivo.
            if (primeiraLinhaComConteudo)
            {
                primeiraLinhaComConteudo = false;
                if (textoQuantidade.Length > 0 && !quantidadeValida)
                    continue;
            }

            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException($"Linha {bruta.NumeroLinha}: nome do produto é obrigatório.");

            if (!quantidadeValida)
                throw new DomainException(
                    $"Linha {bruta.NumeroLinha}: quantidade inválida" +
                    (textoQuantidade.Length > 0 ? $" ('{textoQuantidade}')." : "."));

            resultado.Add(new LinhaImportacaoVendaExternaDto
            {
                NumeroLinha = bruta.NumeroLinha,
                CodigoBarras = codigo.Length == 0 ? null : codigo,
                NomeProduto = nome,
                Quantidade = quantidade
            });
        }

        if (resultado.Count == 0)
            throw new DomainException($"Nenhum item válido encontrado no arquivo {rotuloFormato}.");

        return resultado;
    }

    private static bool TentarLerQuantidade(string texto, out decimal quantidade)
    {
        quantidade = 0m;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        // Vírgula decimal é o que o operador digita ("1,5") e o que a planilha devolve numa
        // máquina em português — a cultura invariante do parse não a aceitaria.
        return decimal.TryParse(
                   texto.Replace(',', '.'),
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out quantidade)
               && quantidade > 0;
    }

    private static char DetectarSeparador(IEnumerable<string> linhas)
    {
        var referencia = linhas.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith('#'));
        return referencia is not null && referencia.Contains(';') ? ';' : ',';
    }

    /// <summary>
    /// Separa os campos respeitando aspas duplas — um nome de produto com o próprio
    /// separador dentro ("Tinta Branca, 18L" num arquivo separado por vírgula) é
    /// exatamente o que o Excel gera ao exportar, e sem isso a linha quebraria em quatro
    /// campos e o item entraria com o nome cortado pela metade.
    /// </summary>
    private static List<string> SepararCsv(string linha, char separador)
    {
        var campos = new List<string>();
        var campo = new StringBuilder();
        var dentroDeAspas = false;

        for (var i = 0; i < linha.Length; i++)
        {
            var caractere = linha[i];

            if (dentroDeAspas)
            {
                if (caractere != '"')
                {
                    campo.Append(caractere);
                }
                else if (i + 1 < linha.Length && linha[i + 1] == '"')
                {
                    campo.Append('"'); // aspas escapadas ("") dentro do campo
                    i++;
                }
                else
                {
                    dentroDeAspas = false;
                }
            }
            else if (caractere == '"')
            {
                dentroDeAspas = true;
            }
            else if (caractere == separador)
            {
                campos.Add(campo.ToString());
                campo.Clear();
            }
            else
            {
                campo.Append(caractere);
            }
        }

        campos.Add(campo.ToString());
        return campos;
    }
}
