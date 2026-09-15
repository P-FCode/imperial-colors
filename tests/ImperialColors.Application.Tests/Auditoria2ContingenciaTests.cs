using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Infrastructure.Contingency;
using ImperialColors.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// AUDITORIA (15/09, segunda rodada) — modo de contingência offline (SQLite) e sincronização.
/// Usa SEMPRE um arquivo SQLite temporário (ver <see cref="Auditoria2Infra"/>).
/// </summary>
public class Auditoria2ContingenciaTests
{
    /// <summary>
    /// CORRIGIDO (C1). Antes, qualquer exceção com "Npgsql" no nome do tipo era tratada como queda
    /// de conexão — e PostgresException é o tipo de TODO erro de dado do Postgres. Um nome de
    /// comprador longo marcava o PDV como offline e "salvava" a venda no SQLite para sempre.
    /// Agora o nome longo é recusado com mensagem clara antes de chegar ao banco.
    /// </summary>
    [Fact]
    public async Task NomeDeCompradorLongo_EhRecusadoComMensagemENaoCaiEmContingencia()
    {
        var (erro, online, pendentes, numero) = await TentarVendaAsync(dto => dto.NomeCompradorAvulso = "Cliente " + new string('X', 240));
        if (erro is SkipException) return;

        Assert.True(online && pendentes == 0, $"PDV offline: {!online}; pendentes: {pendentes}; número: {numero}.");
        var dominio = Assert.IsType<Domain.Exceptions.DomainException>(erro);
        Assert.Contains("200 caracteres", dominio.Message);
    }

    /// <summary>
    /// CORRIGIDO (C1) — o caminho que a validação não cobre: um erro de dado inesperado
    /// (aqui, o usuário da venda maior que a coluna varchar(100)) chega ao Postgres, que responde
    /// SQLSTATE 22001. Isso não pode ativar a contingência: a exceção sobe para o operador.
    /// </summary>
    [Fact]
    public async Task ErroDeDadoQueChegaAoPostgres_NaoPodeSerTratadoComoQuedaDeConexao()
    {
        var (erro, online, pendentes, numero) = await TentarVendaAsync(dto => dto.Usuario = new string('U', 150));
        if (erro is SkipException) return;

        Assert.True(online && pendentes == 0 && erro is not null,
            $"PDV marcado offline: {!online}; vendas pendentes no SQLite: {pendentes}; número devolvido ao operador: {numero}.");
        Assert.Contains("22001", erro!.ToString());
    }

    private static async Task<(Exception? Erro, bool Online, int Pendentes, string Numero)> TentarVendaAsync(Action<CriarVendaDto> ajustar)
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return (new SkipException(), true, 0, "(sem banco)");

        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var (cat, marca) = await infra.CriarCatalogoAsync(sufixo);
        var produto = await infra.CriarProdutoAsync(cat, marca, $"Aud2 Tinta Contingencia {sufixo}", 40m, 10m);

        var health = infra.Servico<IDatabaseHealthService>();
        Assert.True(health.IsOnline, "pré-condição: o teste começa com o banco online");

        var dto = new CriarVendaDto
        {
            ConsumidorFinal = false,
            NomeCompradorAvulso = "Cliente Auditoria2",
            Usuario = "auditoria2",
            Pagamentos = [new CriarVendaPagamentoDto { FormaPagamento = FormaPagamento.Pix, Valor = 40m }],
            Itens = [new CriarItemVendaDto { ProdutoId = produto.Id, Quantidade = 1m, PrecoUnitario = 40m }]
        };
        ajustar(dto);

        VendaDto? venda = null;
        Exception? erro = null;
        try
        {
            venda = await infra.Servico<IVendaService>().CriarAsync(dto);
        }
        catch (Exception ex)
        {
            erro = ex;
        }

        if (venda is { Id: > 0 })
            infra.RegistrarVenda(venda.Id);
        if (venda is not null)
            infra.RegistrarMarcadorAuditoria(venda.NumeroVenda);

        var pendentes = await infra.Servico<IContingencyVendaService>().ContarPendentesAsync();
        return (erro, health.IsOnline, pendentes, venda?.NumeroVenda ?? "(exceção)");
    }

    private sealed class SkipException : Exception;

    /// <summary>
    /// CORRIGIDO (M6). Antes, cada ciclo de 15 s gravava um ERROR em logs_auditoria para a mesma
    /// venda travada (~5.760 linhas/dia) e o PDV dizia "subindo automaticamente". Agora o erro é
    /// registrado uma vez, fica visível nas pendências e a venda sobe sozinha quando a causa é
    /// corrigida (aqui: entrada do estoque que faltou).
    /// </summary>
    [Fact]
    public async Task VendaOfflineRecusada_RegistraOErroUmaVezEResolveQuandoACausaECorrigida()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var (cat, marca) = await infra.CriarCatalogoAsync(sufixo);
        var produto = await infra.CriarProdutoAsync(cat, marca, $"Aud2 Tinta Sync {sufixo}", 25m, 1m);

        var offline = await infra.Servico<IContingencyVendaService>().SalvarVendaOfflineAsync(new CriarVendaDto
        {
            ConsumidorFinal = true,
            Usuario = "auditoria2",
            Pagamentos = [new CriarVendaPagamentoDto { FormaPagamento = FormaPagamento.Pix, Valor = 125m }],
            Itens = [new CriarItemVendaDto { ProdutoId = produto.Id, Quantidade = 5m, PrecoUnitario = 25m }]
        });
        infra.RegistrarMarcadorAuditoria(offline.NumeroVenda);

        infra.RegistrarMarcadorAuditoria(produto.Nome);

        var sync = infra.Servico<DataSyncService>();
        var contingencia = infra.Servico<IContingencyVendaService>();
        for (var i = 0; i < 3; i++)
            await sync.SincronizarPendentesAsync();

        await using (var ctx = await infra.ContextFactory.CreateDbContextAsync())
        {
            var logsErro = await ctx.LogsAuditoria.AsNoTracking()
                .CountAsync(l => l.Acao == "ERRO_SINCRONIZACAO_CONTINGENCIA" && l.Descricao.Contains(offline.NumeroVenda));
            Assert.Equal(1, logsErro);
        }

        var pendencia = Assert.Single(await contingencia.ObterPendenciasComErroAsync());
        Assert.Equal(offline.NumeroVenda, pendencia.NumeroTemporario);
        Assert.Contains("Estoque insuficiente", pendencia.Erro);

        await infra.Servico<IProdutoService>().RegistrarMovimentacaoAsync(new MovimentacaoEstoqueDto
        {
            ProdutoId = produto.Id, Tipo = TipoMovimentacao.Entrada, Quantidade = 10m, Motivo = "Reposição", Usuario = "auditoria2"
        });

        Assert.Equal(1, await sync.SincronizarPendentesAsync());

        await using var local = await infra.Servico<IDbContextFactory<ContingencyDbContext>>().CreateDbContextAsync();
        var sincronizada = await local.VendasContingencia.AsNoTracking().SingleAsync();
        infra.RegistrarVenda(sincronizada.VendaServidorId!.Value);

        Assert.False(sincronizada.PendenteSincronizacao);
        Assert.Null(sincronizada.ErroSincronizacao);
        Assert.Empty(await contingencia.ObterPendenciasComErroAsync());
        Assert.Equal(6m, await infra.EstoqueAsync(produto.Id));

        await using var servidor = await infra.ContextFactory.CreateDbContextAsync();
        var numeroServidor = await servidor.Vendas.AsNoTracking()
            .Where(v => v.Id == sincronizada.VendaServidorId).Select(v => v.NumeroVenda).SingleAsync();
        infra.RegistrarMarcadorAuditoria(numeroServidor);
    }
}
