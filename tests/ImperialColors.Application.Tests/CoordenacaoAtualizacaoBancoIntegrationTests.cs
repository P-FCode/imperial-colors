using ImperialColors.Domain.Constants;
using ImperialColors.Domain.Helpers;
using ImperialColors.Infrastructure.Atualizacao;
using ImperialColors.Infrastructure.Data;
using ImperialColors.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// A trava de coordenação passa por <see cref="ParametroSistemaRepository"/>, que a versão
/// mockada dos outros testes não exercita. Este teste confirma que o valor gravado sobrevive
/// à volta pelo Postgres de verdade — coluna certa, tamanho suficiente.
///
/// Parte de um baseline gravado explicitamente (não do estado que já existir no banco de
/// desenvolvimento): a própria aplicação real, se algum dia rodar nesta máquina depois desta
/// funcionalidade existir, grava essa MESMA chave — depender do estado alheio tornaria o
/// teste refém da ordem de execução e do histórico da máquina de quem o roda.
///
/// Dentro de uma transação revertida; não deixa rastro no banco de desenvolvimento.
/// </summary>
public class CoordenacaoAtualizacaoBancoIntegrationTests
{
    [Fact]
    public async Task RegistroSobreviveAoRoundTripPeloPostgresReal()
    {
        if (!IntegrationTestGuard.TryObterConnectionString(out var conexao))
            return;

        var opcoes = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(conexao).Options;
        await using var ctx = new AppDbContext(opcoes);
        await using var tx = await ctx.Database.BeginTransactionAsync();

        var repositorio = new ParametroSistemaRepository(new FactoryDeContextoFixo(ctx));
        var servico = new CoordenacaoAtualizacaoBancoService(
            repositorio, NullLogger<CoordenacaoAtualizacaoBancoService>.Instance);

        // Baseline conhecido e propositalmente muito antigo — garante que esta instalação
        // seja "mais nova" que o registro não importa qual versão ObterVersaoInstalada()
        // resolva neste processo de teste.
        var registroAntigo = RegistroVersaoBancoHelper.Formatar(new Version(0, 0, 1), "MAQUINA-DE-TESTE-ANTIGA");
        await repositorio.SalvarTextoAsync(ParametroSistemaChaves.VersaoBancoAplicada, registroAntigo);

        var resultado = await servico.VerificarERegistrarAsync();

        Assert.False(resultado.BancoAtualizadoPorOutraInstalacaoMaisNova);
        Assert.Equal(new Version(0, 0, 1), resultado.VersaoRegistradaNoBanco);
        Assert.Equal("MAQUINA-DE-TESTE-ANTIGA", resultado.MaquinaQueAtualizouPorUltimo);

        var gravado = await repositorio.ObterTextoAsync(ParametroSistemaChaves.VersaoBancoAplicada);
        Assert.True(RegistroVersaoBancoHelper.TentarConverter(gravado, out var versaoGravada, out var maquinaGravada));
        Assert.Equal(resultado.VersaoInstalada, versaoGravada);
        Assert.Equal(Environment.MachineName, maquinaGravada);

        // Segunda leitura, agora empatada com o que acabou de gravar: idempotente, não regrava
        // (o registro marca quem AVANÇOU a versão, não vira um log de todo acesso).
        var segundo = await servico.VerificarERegistrarAsync();
        Assert.False(segundo.BancoAtualizadoPorOutraInstalacaoMaisNova);
        Assert.Equal(resultado.VersaoInstalada, segundo.VersaoRegistradaNoBanco);

        await tx.RollbackAsync();
    }
}
