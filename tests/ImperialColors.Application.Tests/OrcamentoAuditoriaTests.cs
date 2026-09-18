using ImperialColors.Application.DTOs;
using ImperialColors.Application.Extensions;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// AUDITORIA (15/09) — testes caixa-preta contra <c>IOrcamentoService</c> escritos para
/// verificar regras de negócio que o módulo (adicionado em 6fc4cbe, por outra ferramenta)
/// declara ter mas que a leitura do código não confirmava na época. Cada teste abaixo afirma o
/// comportamento CORRETO esperado.
///
/// ATUALIZAÇÃO (mesmo dia, após aprovação do cliente): o crítico (concorrência) e os médios
/// (vencimento/máquina de estados) foram corrigidos — ver OrcamentoRepository.
/// RegistrarTransacionalAsync (advisory lock) e OrcamentoService.AlterarStatusAsync (validação
/// de estado/vencimento). Os 3 primeiros testes agora PASSAM. Sanitização de texto continua
/// como achado de baixa severidade, não corrigido nesta rodada — o último teste ainda falha de
/// propósito.
///
/// Limpa tudo que cria (diferente de <c>OrcamentoIntegrationTests.cs</c>, que não limpava —
/// já corrigido também).
/// </summary>
public class OrcamentoAuditoriaTests
{
    private static bool TryConfigurar(out ServiceProvider provider)
    {
        provider = null!;
        if (!IntegrationTestGuard.TryObterConnectionString(out var cs))
            return false;

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(cs);
        services.AddApplication();
        provider = services.BuildServiceProvider();
        return true;
    }

    private static async Task LimparAsync(IDbContextFactory<AppDbContext> contextFactory, params int[] orcamentoIds)
    {
        if (orcamentoIds.Length == 0) return;
        await using var ctx = await contextFactory.CreateDbContextAsync();
        await ctx.Set<ItemOrcamento>().IgnoreQueryFilters()
            .Where(i => orcamentoIds.Contains(i.OrcamentoId)).ExecuteDeleteAsync();
        await ctx.Set<Orcamento>().IgnoreQueryFilters()
            .Where(o => orcamentoIds.Contains(o.Id)).ExecuteDeleteAsync();
    }

    /// <summary>
    /// CORRIGIDO (15/09): <c>OrcamentoService.AlterarStatusAsync</c> agora recusa aprovar um
    /// orçamento cuja validade já passou (<c>DomainException</c>). Antes, nada impedia — um
    /// vendedor podia confirmar, dias depois do vencimento, preços que a loja já tinha
    /// reajustado.
    /// </summary>
    [Fact]
    public async Task AprovarOrcamentoVencido_DeveriaSerBloqueado()
    {
        if (!TryConfigurar(out var provider)) return;
        await using var scope = provider.CreateAsyncScope();
        var orcamentoService = scope.ServiceProvider.GetRequiredService<IOrcamentoService>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        // RegistrarAsync recusa DataValidade no passado (checado na criação) — para obter um
        // orçamento JÁ VENCIDO é preciso criar válido e avançar o relógio por fora do serviço,
        // exatamente o que acontece na vida real: ele nasce válido e vence depois, sem
        // ninguém "tocar" nele.
        var orcamento = await orcamentoService.RegistrarAsync(new RegistrarOrcamentoDto
        {
            NomeCliente = "Auditoria - Vencido",
            DataValidade = DateTime.Today.AddDays(1),
            Itens = [new ItemOrcamentoEntradaDto { NomeProduto = "Item", Quantidade = 1m, PrecoUnitario = 10m }]
        });

        try
        {
            await using (var ctx = await contextFactory.CreateDbContextAsync())
            {
                var entidade = await ctx.Set<Orcamento>().FirstAsync(o => o.Id == orcamento.Id);
                entidade.DataValidade = DateTime.Today.AddDays(-5);
                await ctx.SaveChangesAsync();
            }

            var antes = await orcamentoService.ObterPorIdAsync(orcamento.Id);
            Assert.True(antes!.Expirado, "pré-condição: o orçamento precisa estar vencido antes do teste valer algo");

            await Assert.ThrowsAsync<DomainException>(
                () => orcamentoService.AlterarStatusAsync(orcamento.Id, StatusOrcamento.Aprovado));
        }
        finally
        {
            await LimparAsync(contextFactory, orcamento.Id);
        }
    }

    /// <summary>
    /// CORRIGIDO (15/09): uma vez decidido, o orçamento não pode mais trocar de status —
    /// <c>AlterarStatusAsync</c> agora recusa qualquer alteração quando o status atual não é
    /// mais <c>Aberto</c>. Antes, um orçamento já <c>Recusado</c> podia virar <c>Aprovado</c>
    /// livremente (e vice-versa), sem aviso nem bloqueio.
    /// </summary>
    [Fact]
    public async Task AprovarOrcamentoJaRecusado_DeveriaSerBloqueado()
    {
        if (!TryConfigurar(out var provider)) return;
        await using var scope = provider.CreateAsyncScope();
        var orcamentoService = scope.ServiceProvider.GetRequiredService<IOrcamentoService>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        var orcamento = await orcamentoService.RegistrarAsync(new RegistrarOrcamentoDto
        {
            NomeCliente = "Auditoria - Recusado depois Aprovado",
            DataValidade = DateTime.Today.AddDays(5),
            Itens = [new ItemOrcamentoEntradaDto { NomeProduto = "Item", Quantidade = 1m, PrecoUnitario = 10m }]
        });

        try
        {
            var recusado = await orcamentoService.AlterarStatusAsync(orcamento.Id, StatusOrcamento.Recusado);
            Assert.Equal(StatusOrcamento.Recusado, recusado.Status);

            await Assert.ThrowsAsync<DomainException>(
                () => orcamentoService.AlterarStatusAsync(orcamento.Id, StatusOrcamento.Aprovado));
        }
        finally
        {
            await LimparAsync(contextFactory, orcamento.Id);
        }
    }

    /// <summary>
    /// REVISTO (segunda rodada). A recomendação original era aplicar
    /// <c>InputSanitizer.SanitizarTexto</c> aqui, porque o sanitizador apagava
    /// <c>&lt; &gt; &amp; " '</c> nos cadastros e o Orçamento só fazia <c>.Trim()</c>.
    /// A divergência foi resolvida no outro sentido: o sanitizador parou de apagar esses
    /// caracteres (ver <c>Auditoria2CadastroTests.SanitizarTexto_NaoDeveApagarCaracteresLegitimosDeCadastro</c>),
    /// porque são dado legítimo e o app não tem superfície HTML. O contrato agora é o mesmo
    /// nos dois lados: o nome digitado no balcão chega íntegro ao orçamento e ao PDF.
    /// </summary>
    [Fact]
    public async Task NomeClienteComCaracteresEspeciais_DeveChegarIntegroAoOrcamento()
    {
        if (!TryConfigurar(out var provider)) return;
        await using var scope = provider.CreateAsyncScope();
        var orcamentoService = scope.ServiceProvider.GetRequiredService<IOrcamentoService>();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        var orcamento = await orcamentoService.RegistrarAsync(new RegistrarOrcamentoDto
        {
            NomeCliente = "<script>alert(1)</script> João \"Pintor\"",
            DataValidade = DateTime.Today.AddDays(5),
            Itens = [new ItemOrcamentoEntradaDto { NomeProduto = "Item", Quantidade = 1m, PrecoUnitario = 10m }]
        });

        try
        {
            Assert.Equal("<script>alert(1)</script> João \"Pintor\"", orcamento.NomeCliente);
        }
        finally
        {
            await LimparAsync(contextFactory, orcamento.Id);
        }
    }

    /// <summary>
    /// CORRIGIDO (15/09): a geração do número de orçamento foi movida para dentro da
    /// transação de <c>RegistrarTransacionalAsync</c>, protegida por
    /// <c>pg_advisory_xact_lock</c> — o mesmo padrão já usado em
    /// <c>VendaRepository.CriarComBaixaEstoqueTransacionalAsync</c>. Antes, o número era lido
    /// fora de qualquer lock (MAX atual + 1); o índice único impedia a duplicata no banco, mas
    /// isso significava que a SEGUNDA gravação concorrente estourava com
    /// <c>DbUpdateException</c> cru até o operador.
    /// </summary>
    [Fact]
    public async Task RegistrarDoisOrcamentosSimultaneamente_NenhumDeveFalhar()
    {
        if (!TryConfigurar(out var provider)) return;
        var contextFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        RegistrarOrcamentoDto NovoDto(string sufixo) => new()
        {
            NomeCliente = $"Auditoria - Concorrência {sufixo}",
            DataValidade = DateTime.Today.AddDays(5),
            Itens = [new ItemOrcamentoEntradaDto { NomeProduto = "Item", Quantidade = 1m, PrecoUnitario = 10m }]
        };

        // Cada "PDV" com seu próprio scope/serviço, como dois caixas de verdade teriam.
        async Task<(bool Sucesso, OrcamentoDto? Orcamento, Exception? Erro)> TentarAsync(string sufixo)
        {
            await using var scope = provider.CreateAsyncScope();
            var servico = scope.ServiceProvider.GetRequiredService<IOrcamentoService>();
            try
            {
                var criado = await servico.RegistrarAsync(NovoDto(sufixo));
                return (true, criado, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex);
            }
        }

        var resultados = await Task.WhenAll(
            TentarAsync("A"), TentarAsync("B"), TentarAsync("C"), TentarAsync("D"), TentarAsync("E"));

        var criados = resultados.Where(r => r.Orcamento is not null).Select(r => r.Orcamento!.Id).ToArray();
        try
        {
            var falhas = resultados.Where(r => !r.Sucesso).ToList();
            Assert.True(falhas.Count == 0,
                "esperava que os 5 registros concorrentes tivessem sucesso (ou tentassem de novo sozinhos); " +
                $"{falhas.Count} lançaram exceção não tratada: {string.Join(" | ", falhas.Select(f => f.Erro!.GetType().Name + ": " + f.Erro.Message))}");

            var numeros = resultados.Where(r => r.Orcamento is not null).Select(r => r.Orcamento!.NumeroOrcamento).ToList();
            Assert.Equal(numeros.Count, numeros.Distinct().Count());
        }
        finally
        {
            await LimparAsync(contextFactory, criados);
        }
    }
}
