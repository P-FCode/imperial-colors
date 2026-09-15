using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Enums;
using Xunit;

namespace ImperialColors.Application.Tests;

/// <summary>
/// AUDITORIA (15/09, segunda rodada) — cadastros (cliente/produto) e higienização de texto.
/// </summary>
public class Auditoria2CadastroTests
{
    private static string CpfValidoAleatorio()
    {
        var n = Enumerable.Range(0, 9).Select(_ => Random.Shared.Next(0, 10)).ToList();
        int Dv(IReadOnlyList<int> d, int pesoInicial)
        {
            var soma = d.Select((x, i) => x * (pesoInicial - i)).Sum();
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }
        n.Add(Dv(n, 10));
        n.Add(Dv(n, 11));
        var s = string.Concat(n);
        return $"{s[..3]}.{s[3..6]}.{s[6..9]}-{s[9..]}";
    }

    /// <summary>
    /// InputSanitizer.SanitizarTexto remove &lt; &gt; &amp; " ' — proteção de HTML que não faz
    /// sentido num app WPF (nada é renderizado como HTML) e APAGA dado legítimo de uma loja
    /// de tintas: medida em polegadas, razão social com &amp;, sobrenome com apóstrofo. O mesmo
    /// nome vai para cupom e para a NF-e sem os caracteres.
    ///
    /// Importante: isto inverte a recomendação da primeira rodada ("aplicar o sanitizador no
    /// Orçamento") — aplicar espalharia a perda de dados.
    /// </summary>
    [Theory]
    [InlineData("Trincha 2\" Atlas")]
    [InlineData("Rolo de Lã 23cm 9\"")]
    [InlineData("Silva & Filhos Tintas LTDA")]
    [InlineData("Maria D'Ávila")]
    public void SanitizarTexto_NaoDeveApagarCaracteresLegitimosDeCadastro(string original)
    {
        Assert.Equal(original, InputSanitizer.SanitizarTexto(original, 200));
    }

    /// <summary>
    /// CORRIGIDO (M5). Antes a coluna clientes.numero (varchar 10) recebia até 20 caracteres do
    /// serviço e a tela não tinha limite: estourava com a mensagem técnica do Postgres. Agora o
    /// serviço usa o limite da coluna e a tela não deixa digitar além disso.
    /// </summary>
    [Fact]
    public async Task ClienteComNumeroDeEnderecoDe16Caracteres_NaoDeveEstourarNoBanco()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        ClienteDto? criado = null;
        var erro = await Record.ExceptionAsync(async () =>
        {
            criado = await infra.Servico<IClienteService>().CriarAsync(new ClienteDto
            {
                TipoPessoa = TipoPessoa.Fisica,
                Nome = "Auditoria2 Numero Longo",
                Numero = "Km 12 S/N Fundos"
            });
        });
        if (criado is not null) infra.RegistrarCliente(criado.Id);

        Assert.True(erro is null || !erro.Message.Contains("Erro real do banco"),
            $"mensagem mostrada ao operador: {erro?.Message}");
    }

    /// <summary>
    /// CORRIGIDO (M5). Antes a edição copiava o texto cru e estourava no banco com dados que a
    /// criação aceitava. Agora criação e edição passam pela mesma normalização.
    /// </summary>
    [Fact]
    public async Task EditarCliente_DeveAplicarAsMesmasRegrasDaCriacao()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        var servico = infra.Servico<IClienteService>();
        var criado = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Edicao" });
        infra.RegistrarCliente(criado.Id);

        var nomeLongo = "Auditoria2 " + new string('N', 250);
        var aoCriar = InputSanitizer.SanitizarTexto(nomeLongo, 200);

        var erro = await Record.ExceptionAsync(() => servico.AtualizarAsync(criado.Id, new ClienteDto
        {
            TipoPessoa = TipoPessoa.Fisica,
            Nome = nomeLongo
        }));
        var depois = await servico.ObterPorIdAsync(criado.Id);

        Assert.True(erro is null && depois!.Nome == aoCriar,
            $"criação trunca para {aoCriar.Length} caracteres; edição → {erro?.GetType().Name ?? "ok"}: {erro?.Message}");
    }

    /// <summary>
    /// Não-funcional. A busca do PDV (TxtBuscaProduto_TextChanged, a cada tecla a partir de 2
    /// caracteres) chama ProdutoService.BuscarAsync, que monta ILIKE '%termo%' sem escapar os
    /// curingas do próprio LIKE e sem limite de linhas. Digitar "%" ou "_" (ex.: "50%") traz o
    /// catálogo inteiro, com Includes de categoria/marca/fornecedor, para dentro do popup.
    /// </summary>
    [Fact]
    public async Task BuscaDeProduto_ComCaractereCuringa_NaoDeveDevolverOCatalogoInteiro()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var (cat, marca) = await infra.CriarCatalogoAsync(sufixo);
        var produto = await infra.CriarProdutoAsync(cat, marca, $"Aud2 Massa Corrida {sufixo}", 10m, 1m);

        var inicio = System.Diagnostics.Stopwatch.StartNew();
        var resultado = (await infra.Servico<IProdutoService>().BuscarAsync("%_")).ToList();
        inicio.Stop();

        Assert.True(resultado.All(p => p.Id != produto.Id),
            $"busca por \"%_\" devolveu {resultado.Count} produto(s) em {inicio.ElapsedMilliseconds} ms, " +
            "incluindo um produto sem nenhum desses caracteres no nome.");
    }

    /// <summary>
    /// CORRIGIDO (B1). Antes o mesmo CPF podia ser cadastrado várias vezes, espalhando o histórico
    /// de compras entre fichas diferentes.
    /// </summary>
    [Fact]
    public async Task CadastrarDoisClientesComOMesmoCpf_DeveSerBloqueado()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        var servico = infra.Servico<IClienteService>();
        var cpf = CpfValidoAleatorio();

        var primeiro = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 CPF 1", Cpf = cpf });
        infra.RegistrarCliente(primeiro.Id);

        ClienteDto? segundo = null;
        var erro = await Record.ExceptionAsync(async () =>
            segundo = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 CPF 2", Cpf = cpf }));
        if (segundo is not null) infra.RegistrarCliente(segundo.Id);

        Assert.True(erro is not null, $"CPF {cpf} cadastrado duas vezes (Ids {primeiro.Id} e {segundo?.Id}).");
        Assert.Contains("Auditoria2 CPF 1", erro!.Message);
    }

    /// <summary>
    /// B1: a comparação é pelos dígitos (cadastros antigos têm documento com e sem máscara) e a
    /// edição do próprio cliente, mantendo o mesmo documento, continua permitida.
    /// </summary>
    [Fact]
    public async Task DocumentoDuplicado_ComparaSoDigitosEPermiteEditarOProprioCliente()
    {
        await using var infra = await Auditoria2Infra.CriarAsync();
        if (infra is null) return;

        var servico = infra.Servico<IClienteService>();
        var cpf = CpfValidoAleatorio();
        var cpfSemMascara = new string(cpf.Where(char.IsDigit).ToArray());

        var cliente = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Mascara", Cpf = cpf });
        infra.RegistrarCliente(cliente.Id);

        ClienteDto? outro = null;
        var erro = await Record.ExceptionAsync(async () =>
            outro = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Sem Mascara", Cpf = cpfSemMascara }));
        if (outro is not null) infra.RegistrarCliente(outro.Id);

        Assert.IsType<Domain.Exceptions.DomainException>(erro);

        var editado = await servico.AtualizarAsync(cliente.Id, new ClienteDto
        {
            TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Mascara (editado)", Cpf = cpfSemMascara
        });
        Assert.Equal("Auditoria2 Mascara (editado)", editado.Nome);

        var segundo = await servico.CriarAsync(new ClienteDto { TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Outro", Cpf = CpfValidoAleatorio() });
        infra.RegistrarCliente(segundo.Id);

        var trocaParaCpfExistente = await Record.ExceptionAsync(() => servico.AtualizarAsync(segundo.Id, new ClienteDto
        {
            TipoPessoa = TipoPessoa.Fisica, Nome = "Auditoria2 Outro", Cpf = cpf
        }));
        Assert.IsType<Domain.Exceptions.DomainException>(trocaParaCpfExistente);
    }
}
