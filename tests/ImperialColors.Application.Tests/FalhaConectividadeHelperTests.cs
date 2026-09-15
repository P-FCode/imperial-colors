using System.Net.Sockets;
using ImperialColors.Application.Helpers;
using ImperialColors.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// C1 da auditoria de 15/09: só queda/indisponibilidade do banco pode mandar a venda para a
/// contingência offline. Erro de dado recusado pelo Postgres precisa chegar ao operador.
/// </summary>
public class FalhaConectividadeHelperTests
{
    private static PostgresException Postgres(string sqlState) => new("mensagem do servidor", "ERROR", "ERROR", sqlState);

    [Theory]
    [InlineData("22001")] // texto maior que a coluna
    [InlineData("22003")] // valor numérico fora do intervalo
    [InlineData("23503")] // chave estrangeira
    [InlineData("23505")] // índice único
    [InlineData("40P01")] // deadlock
    [InlineData("57014")] // comando cancelado por statement_timeout
    public void ErroDeDadoOuRegraDoPostgres_NaoEhFalhaDeConectividade(string sqlState)
    {
        var excecao = new DbUpdateException("An error occurred while saving the entity changes.", Postgres(sqlState));

        Assert.False(FalhaConectividadeHelper.EhFalhaDeConectividade(excecao));
    }

    [Theory]
    [InlineData("08006")] // conexão perdida
    [InlineData("08001")] // não foi possível conectar
    [InlineData("57P01")] // servidor desligando
    [InlineData("57P03")] // servidor iniciando/recuperando
    [InlineData("53300")] // conexões esgotadas
    [InlineData("28P01")] // senha do banco recusada
    [InlineData("3D000")] // banco inexistente
    public void ServidorIndisponivel_EhFalhaDeConectividade(string sqlState)
    {
        Assert.True(FalhaConectividadeHelper.EhFalhaDeConectividade(Postgres(sqlState)));
    }

    [Fact]
    public void FalhaDeRedeAntesDeOServidorResponder_EhFalhaDeConectividade()
    {
        var rede = new NpgsqlException("Failed to connect to 127.0.0.1:5432", new SocketException(10061));
        var retryEsgotado = new RetryLimitExceededException("Maximum number of retries exceeded.", rede);

        Assert.True(FalhaConectividadeHelper.EhFalhaDeConectividade(rede));
        Assert.True(FalhaConectividadeHelper.EhFalhaDeConectividade(retryEsgotado));
        Assert.True(FalhaConectividadeHelper.EhFalhaDeConectividade(new NpgsqlException("Exception while reading from stream", new TimeoutException())));
        Assert.True(FalhaConectividadeHelper.EhFalhaDeConectividade(new IOException("connection reset")));
    }

    [Fact]
    public void ErroDeNegocioOuMensagemComPalavraConnection_NaoEhFalhaDeConectividade()
    {
        Assert.False(FalhaConectividadeHelper.EhFalhaDeConectividade(new DomainException("Estoque insuficiente.")));
        Assert.False(FalhaConectividadeHelper.EhFalhaDeConectividade(new InvalidOperationException("connection string timeout network")));
        Assert.False(FalhaConectividadeHelper.EhFalhaDeConectividade(null));
    }
}
