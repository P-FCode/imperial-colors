using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

public class ProdutoCodigoInternoIntegrationTests
{
    [Fact]
    public async Task CriarTresProdutosSeguidos_DeveGerarCodigosSequenciaisUnicos()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();

        await using var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var produtoService = scope.ServiceProvider.GetRequiredService<IProdutoService>();
        var categoriaService = scope.ServiceProvider.GetRequiredService<ICategoriaService>();
        var marcaService = scope.ServiceProvider.GetRequiredService<IMarcaService>();

        var categorias = (await categoriaService.ObterTodosAsync()).ToList();
        var marcas = (await marcaService.ObterTodosAsync()).ToList();

        if (categorias.Count == 0 || marcas.Count == 0)
            return;

        var maiorAntes = (await produtoService.ObterTodosAsync())
            .Select(p => p.CodigoInterno)
            .Where(c => c.StartsWith('P') && int.TryParse(c[1..], out _))
            .Select(c => int.Parse(c[1..]))
            .DefaultIfEmpty(0)
            .Max();

        var codigosGerados = new List<string>();
        var idsCriados = new List<int>();

        try
        {
            for (var i = 0; i < 3; i++)
            {
                var codigoSugerido = await produtoService.GerarProximoCodigoInternoAsync();
                var criado = await produtoService.CriarAsync(new CriarProdutoDto
                {
                    CodigoInterno = codigoSugerido,
                    CodigoInternoDefinidoManualmente = false,
                    Nome = $"Produto Stress {Guid.NewGuid():N}",
                    CategoriaId = categorias[0].Id,
                    MarcaId = marcas[0].Id,
                    Custo = 10m,
                    PrecoVenda = 20m,
                    QuantidadeEstoque = 0,
                    EstoqueMinimo = 0,
                    Unidade = "UN"
                });

                codigosGerados.Add(criado.CodigoInterno);
                idsCriados.Add(criado.Id);
            }

            Assert.Equal(3, codigosGerados.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(codigosGerados, c => Assert.Matches("^P[0-9]{5}$", c));
            Assert.All(codigosGerados, c => Assert.True(int.Parse(c[1..]) > maiorAntes || maiorAntes == 0));
        }
        finally
        {
            foreach (var id in idsCriados)
            {
                try { await produtoService.RemoverAsync(id); }
                catch { /* ignore */ }
            }
        }
    }
}
