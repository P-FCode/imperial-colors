namespace ImperialColors.Application.Helpers;

public static class ProdutoPrecoHelper
{
    public static bool EstaEmPromocao(bool promocaoAtiva, decimal? precoPromocional, decimal precoVenda)
        => promocaoAtiva
           && precoPromocional.HasValue
           && precoPromocional.Value > 0
           && precoPromocional.Value < precoVenda;

    public static decimal ObterPrecoEfetivo(decimal precoVenda, bool promocaoAtiva, decimal? precoPromocional)
        => EstaEmPromocao(promocaoAtiva, precoPromocional, precoVenda)
            ? precoPromocional!.Value
            : precoVenda;
}
