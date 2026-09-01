using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// <c>ObterEstatisticasAsync</c> monta o painel-resumo do hub de Nota Fiscal a partir de três
/// agregações (por status, por tipo, por período) — a consulta certa mas o bucket errado (uma
/// nota Denegada contada como Pendente, por exemplo) não dá erro nenhum, só mostra um número
/// enganoso na tela. Roda contra o Postgres real porque o que está sendo testado é a tradução
/// exata de <c>StatusNotaFiscal</c>/<c>TipoNotaFiscal</c> para os buckets do read model, algo
/// que um mock não exercitaria de verdade.
///
/// A consulta soma a tabela inteira, sem filtro por série — por isso o teste começa limpando
/// <c>notas_fiscais</c> (cascata cobre Itens/Pagamentos/Eventos) dentro da própria transação
/// revertida, para os números ficarem exatos independente do que já exista no banco de
/// desenvolvimento.
/// </summary>
public class NotaFiscalEstatisticasIntegrationTests
{
    private static NotaFiscal Nota(
        TipoNotaFiscal tipo, StatusNotaFiscal status, decimal valor, DateTime dataEmissao, string numero) => new()
    {
        Tipo = tipo,
        Status = status,
        Serie = "998",
        Numero = numero,
        Ambiente = AmbienteEmissaoFiscal.Producao,
        DataEmissao = dataEmissao,
        VNf = valor,
        Crt = "1"
    };

    [Fact]
    public async Task ObterEstatisticasAsync_ClassificaCadaNotaNoBucketCorreto()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        // Limpo para os totais ficarem exatos, não "o que já havia + o que este teste inseriu".
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM notas_fiscais");

        var hoje = Relogio.Hoje;
        // Antes do início do mês corrente por construção — não depende de qual dia do mês o
        // teste roda (rodando no dia 1, "ontem" já seria mês anterior; esta data sempre é).
        var mesPassado = new DateTime(hoje.Year, hoje.Month, 1).AddDays(-5);

        ctx.NotasFiscais.AddRange(
            // Autorizadas: uma hoje, uma no mês passado — o suficiente para provar que Hoje/Mês
            // são um recorte do total (a do mês passado entra no total mas fica de fora dos
            // dois), sem depender de existir uma data "este mês mas não hoje" (que não existe
            // quando o teste roda no dia 1).
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Autorizada, 100m, hoje.AddHours(9), "1"),
            Nota(TipoNotaFiscal.NFCe, StatusNotaFiscal.Autorizada, 50m, mesPassado, "2"),
            // Uma nota rejeitada/cancelada/denegada/rascunho/indeterminada — uma de cada, para
            // cada contador ter exatamente 1 e nenhum vazar para o bucket vizinho.
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Rejeitada, 999m, hoje, "4"),
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Cancelada, 999m, hoje, "5"),
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Denegada, 999m, hoje, "6"),
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Rascunho, 999m, hoje, "7"),
            Nota(TipoNotaFiscal.NFe, StatusNotaFiscal.Indeterminada, 999m, hoje, "8"));
        await ctx.SaveChangesAsync();

        var repositorio = new NotaFiscalRepository(new FactoryDeContextoFixo(ctx));
        var estatisticas = await repositorio.ObterEstatisticasAsync();

        // Só as duas Autorizadas entram em Emitidas/Valor — as outras cinco não são
        // documentos fiscais válidos e não podem inflar esse número.
        Assert.Equal(2, estatisticas.TotalEmitidas);
        Assert.Equal(150m, estatisticas.ValorTotalEmitido);

        // A do mês passado fica de fora dos dois recortes de período, mas conta no total acima.
        Assert.Equal(1, estatisticas.EmitidasHoje);
        Assert.Equal(100m, estatisticas.ValorEmitidoHoje);
        Assert.Equal(1, estatisticas.EmitidasNoMes);
        Assert.Equal(100m, estatisticas.ValorEmitidoNoMes);

        Assert.Equal(1, estatisticas.TotalRejeitadas);
        Assert.Equal(1, estatisticas.TotalCanceladas);
        Assert.Equal(1, estatisticas.TotalDenegadas);
        // Pendentes = Rascunho + Indeterminada, uma de cada acima.
        Assert.Equal(2, estatisticas.TotalPendentes);

        // Rejeitada/Cancelada/Denegada/Rascunho/Indeterminada são todas NF-e no fixture, mas
        // NENHUMA delas pode aparecer em TotalNFe — só a Autorizada conta aqui.
        Assert.Equal(1, estatisticas.TotalNFe);
        Assert.Equal(100m, estatisticas.ValorNFe);
        Assert.Equal(1, estatisticas.TotalNFCe);
        Assert.Equal(50m, estatisticas.ValorNFCe);

        await tx.RollbackAsync();
    }

    /// <summary>Banco sem nenhuma nota: toda contagem zero, e nenhuma divisão por zero ou
    /// exceção — é o estado real de uma implantação nova, antes da primeira emissão.</summary>
    [Fact]
    public async Task ObterEstatisticasAsync_SemNenhumaNota_DevolveTudoZerado()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM notas_fiscais");

        var repositorio = new NotaFiscalRepository(new FactoryDeContextoFixo(ctx));
        var estatisticas = await repositorio.ObterEstatisticasAsync();

        Assert.Equal(0, estatisticas.TotalEmitidas);
        Assert.Equal(0m, estatisticas.ValorTotalEmitido);
        Assert.Equal(0, estatisticas.TotalNFe);
        Assert.Equal(0, estatisticas.TotalNFCe);
        Assert.Null(estatisticas.TicketMedio);

        await tx.RollbackAsync();
    }
}
