using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Helpers;

public static class VendaExternaTxtImportHelper
{
    public static IReadOnlyList<LinhaImportacaoVendaExternaDto> ParseArquivo(string conteudo)
    {
        if (string.IsNullOrWhiteSpace(conteudo))
            throw new DomainException("O arquivo TXT está vazio.");

        var linhas = conteudo
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (linhas.Length == 0)
            throw new DomainException("Nenhuma linha válida encontrada no arquivo TXT.");

        var resultado = new List<LinhaImportacaoVendaExternaDto>();

        for (var i = 0; i < linhas.Length; i++)
        {
            var linha = linhas[i];
            if (string.IsNullOrWhiteSpace(linha) || linha.StartsWith('#'))
                continue;

            var partes = linha.Split(';');
            if (partes.Length < 3)
                throw new DomainException($"Linha {i + 1}: formato inválido. Use CODIGO_DE_BARRAS;NOME_DO_PRODUTO;QUANTIDADE.");

            if (!decimal.TryParse(partes[2].Trim().Replace(',', '.'),
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var quantidade) || quantidade <= 0)
                throw new DomainException($"Linha {i + 1}: quantidade inválida.");

            var codigo = partes[0].Trim();
            var nome = partes[1].Trim();
            if (string.IsNullOrWhiteSpace(nome))
                throw new DomainException($"Linha {i + 1}: nome do produto é obrigatório.");

            resultado.Add(new LinhaImportacaoVendaExternaDto
            {
                NumeroLinha = i + 1,
                CodigoBarras = string.IsNullOrWhiteSpace(codigo) ? null : codigo,
                NomeProduto = nome,
                Quantidade = quantidade
            });
        }

        if (resultado.Count == 0)
            throw new DomainException("Nenhum item válido encontrado no arquivo TXT.");

        return resultado;
    }
}
