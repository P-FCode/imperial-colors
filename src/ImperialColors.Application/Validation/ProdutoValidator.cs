using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Exceptions;

namespace ImperialColors.Application.Validation;

public static class ProdutoValidator
{
    public static void Validar(CriarProdutoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoInterno))
            throw new DomainException("Código interno é obrigatório.");

        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new DomainException("Nome do produto é obrigatório.");

        if (!dto.CategoriaId.HasValue || dto.CategoriaId.Value <= 0)
            throw new DomainException("Selecione uma Categoria e uma Marca válidas.");

        if (!dto.MarcaId.HasValue || dto.MarcaId.Value <= 0)
            throw new DomainException("Selecione uma Categoria e uma Marca válidas.");

        if (dto.PrecoVenda <= 0)
            throw new DomainException("Preço de venda deve ser maior que zero.");

        if (dto.Custo is <= 0)
            throw new DomainException("Preço de custo, quando informado, deve ser maior que zero.");

        if (dto.QuantidadeEstoque < 0)
            throw new DomainException("Quantidade em estoque não pode ser negativa.");

        if (dto.EstoqueMinimo < 0)
            throw new DomainException("Estoque mínimo não pode ser negativo.");

        if (string.IsNullOrWhiteSpace(dto.Unidade))
            throw new DomainException("Unidade de medida é obrigatória.");

        if (!UnidadesMedida.EhValida(dto.Unidade))
            throw new DomainException("Unidade de medida inválida. Use: UN, GL, LT, RL, CX ou PCT.");

        ValidarPromocao(dto.PromocaoAtiva, dto.PrecoPromocional, dto.PrecoVenda);
    }

    private static void ValidarPromocao(bool promocaoAtiva, decimal? precoPromocional, decimal precoVenda)
    {
        if (!promocaoAtiva)
            return;

        if (!precoPromocional.HasValue || precoPromocional.Value <= 0)
            throw new DomainException("Informe o preço promocional quando o modo promocional estiver ativo.");

        if (precoPromocional.Value > precoVenda)
            throw new DomainException("O preço promocional não pode ser maior que o preço de venda padrão.");
    }

    public static void Validar(AtualizarProdutoDto dto) => Validar((CriarProdutoDto)dto);
}
