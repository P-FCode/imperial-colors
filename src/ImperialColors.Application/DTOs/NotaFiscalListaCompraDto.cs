namespace ImperialColors.Application.DTOs;

public class NotaFiscalListaCompraDto
{
    public byte[] Conteudo { get; set; } = Array.Empty<byte>();
    public string NomeArquivo { get; set; } = string.Empty;
}
