using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Validation;
using ImperialColors.Domain.Entities;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Interfaces;

namespace ImperialColors.Application.Services;

public class NotaFiscalService : INotaFiscalService
{
    private readonly INotaFiscalRepository _notaFiscalRepository;
    private readonly IVendaRepository _vendaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IProdutoRepository _produtoRepository;
    private readonly ITributacaoProdutoRepository _tributacaoProdutoRepository;
    private readonly ITributacaoCategoriaRepository _tributacaoCategoriaRepository;
    private readonly IRepository<NaturezaOperacao> _naturezaOperacaoRepository;
    private readonly IConfiguracaoFiscalService _configuracaoFiscal;
    private readonly IFiscalApiClient _fiscalApiClient;

    public NotaFiscalService(
        INotaFiscalRepository notaFiscalRepository,
        IVendaRepository vendaRepository,
        IClienteRepository clienteRepository,
        IProdutoRepository produtoRepository,
        ITributacaoProdutoRepository tributacaoProdutoRepository,
        ITributacaoCategoriaRepository tributacaoCategoriaRepository,
        IRepository<NaturezaOperacao> naturezaOperacaoRepository,
        IConfiguracaoFiscalService configuracaoFiscal,
        IFiscalApiClient fiscalApiClient)
    {
        _notaFiscalRepository = notaFiscalRepository;
        _vendaRepository = vendaRepository;
        _clienteRepository = clienteRepository;
        _produtoRepository = produtoRepository;
        _tributacaoProdutoRepository = tributacaoProdutoRepository;
        _tributacaoCategoriaRepository = tributacaoCategoriaRepository;
        _naturezaOperacaoRepository = naturezaOperacaoRepository;
        _configuracaoFiscal = configuracaoFiscal;
        _fiscalApiClient = fiscalApiClient;
    }

    public async Task<NotaFiscalDto> MontarRascunhoAPartirDeVendaAsync(
        int vendaId, TipoNotaFiscal tipo, CancellationToken cancellationToken = default)
    {
        var venda = await _vendaRepository.ObterComItensAsync(vendaId)
            ?? throw new DomainException($"Venda com Id {vendaId} não encontrada.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var crt = await _configuracaoFiscal.ObterCodigoCrtAsync(cancellationToken);
        var serie = string.IsNullOrWhiteSpace(empresa.Serie) ? "1" : empresa.Serie;

        var nota = new NotaFiscalDto
        {
            Tipo = tipo,
            VendaId = venda.Id,
            ClienteId = venda.ClienteId,
            Serie = serie,
            Numero = await _notaFiscalRepository.ObterProximoNumeroAsync(tipo, serie, cancellationToken),
            DataEmissao = DateTime.Now,
            Crt = crt,
            Ambiente = empresa.Ambiente,
            ConsumidorFinal = tipo == TipoNotaFiscal.NFCe || venda.ClienteId is null,
            IndicadorPresenca = empresa.IndicadorPresencaPadrao,
            FormaEnvio = empresa.FretePorContaPadrao,
            Observacoes = venda.Observacoes
        };

        if (venda.ClienteId.HasValue)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(venda.ClienteId.Value);
            if (cliente is not null)
                PreencherDestinatarioDeCliente(nota, cliente);
        }
        else if (!string.IsNullOrWhiteSpace(venda.NomeCompradorCupom))
        {
            nota.DestinatarioNome = venda.NomeCompradorCupom;
            nota.DestinatarioDocumento = venda.DocumentoCompradorCupom;
            nota.DestinatarioTipoPessoa = venda.TipoPessoaComprador;
            nota.DestinatarioIndicadorIe = IndicadorIeDestinatario.NaoContribuinte;
        }

        var interestadual = !string.IsNullOrWhiteSpace(nota.DestinatarioUf) &&
            !string.Equals(nota.DestinatarioUf, empresa.Uf, StringComparison.OrdinalIgnoreCase);

        foreach (var itemVenda in venda.Itens)
        {
            var item = await MontarItemAPartirDeProdutoAsync(itemVenda.ProdutoId, itemVenda.Quantidade, interestadual, cancellationToken);
            item.ValorUnitario = itemVenda.PrecoUnitario;
            item.ValorTotal = itemVenda.Subtotal;
            nota.Itens.Add(item);
        }

        foreach (var pagamentoVenda in venda.Pagamentos)
        {
            nota.Pagamentos.Add(new NotaFiscalPagamentoDto
            {
                FormaPagamento = pagamentoVenda.FormaPagamento,
                Valor = pagamentoVenda.Valor,
                QuantidadeParcelas = pagamentoVenda.QuantidadeParcelas,
                Ordem = pagamentoVenda.Ordem
            });
        }

        return RecalcularTotais(nota);
    }

    public async Task<ItemNotaFiscalDto> MontarItemAPartirDeProdutoAsync(
        int produtoId, decimal quantidade, bool interestadual, CancellationToken cancellationToken = default)
    {
        var produto = await _produtoRepository.ObterPorIdAsync(produtoId)
            ?? throw new DomainException($"Produto com Id {produtoId} não encontrado.");

        var tributacao = await _tributacaoProdutoRepository.ObterPorProdutoIdAsync(produtoId, cancellationToken);
        if (tributacao is null && produto.CategoriaId.HasValue)
        {
            var tributacaoCategoria = await _tributacaoCategoriaRepository.ObterPorCategoriaIdAsync(produto.CategoriaId.Value, cancellationToken);
            if (tributacaoCategoria is not null)
                tributacao = MapearTributacaoCategoriaComoProduto(tributacaoCategoria, produtoId);
        }

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var precoUnitario = ProdutoPrecoHelper.ObterPrecoEfetivo(produto.PrecoVenda, produto.PromocaoAtiva, produto.PrecoPromocional);

        // Sem CST/CSOSN cadastrado no produto (nem herdado da categoria), cai no padrão
        // "Regra Geral" da empresa — mesmo papel que empresa.CstIbsCbsPadrao já cumpre pra
        // IBS/CBS. Sem isso, todo produto sem tributação bloqueava a emissão com "falta CST
        // ou CSOSN do ICMS" (obrigatório em todo item da NF-e, sem exceção — não dá pra
        // simplesmente pular essa validação). Só cai no padrão da empresa quando o produto
        // não define NENHUM dos dois — nunca mistura CST do produto com CSOSN da empresa.
        var temIcmsProduto = !string.IsNullOrWhiteSpace(tributacao?.CstIcms) || !string.IsNullOrWhiteSpace(tributacao?.CsosnIcms);
        var cstIcmsResolvido = temIcmsProduto ? tributacao?.CstIcms : empresa.CstIcmsPadrao;

        var item = new ItemNotaFiscalDto
        {
            ProdutoId = produto.Id,
            CodigoProduto = produto.CodigoInterno,
            CodigoBarras = produto.CodigoBarras,
            Descricao = produto.Nome,
            Unidade = produto.Unidade,
            Quantidade = quantidade,
            ValorUnitario = precoUnitario,
            ValorTotal = Math.Round(quantidade * precoUnitario, 2, MidpointRounding.AwayFromZero),
            Cfop = interestadual ? tributacao?.CfopForaEstado ?? string.Empty : tributacao?.CfopDentroEstado ?? string.Empty,
            Ncm = tributacao?.Ncm,
            Cest = tributacao?.Cest,
            Origem = tributacao?.Origem is null ? "0" : ((int)tributacao.Origem).ToString(),
            CstIcms = cstIcmsResolvido,
            CsosnIcms = temIcmsProduto ? tributacao?.CsosnIcms : empresa.CsosnIcmsPadrao,
            AliquotaIcms = tributacao?.AliquotaIcms,
            // vBC é obrigatório no XML sempre que o CST exige o grupo ICMS00/20/51/90 (leiaute
            // NFe) — independente de a alíquota estar cadastrada ou não. Antes isso dependia de
            // AliquotaIcms.HasValue, então um produto com CST '00' sem alíquota cadastrada saía
            // sem vBC/pICMS/vICMS no JSON, e a API/SEFAZ rejeitava por XSD_VALIDATION
            // ("incomplete content... expected 'pICMS'"). NotaFiscalValidator.ValidarIcmsItem
            // bloqueia a emissão nesse cenário (falta alíquota) antes de chegar aqui de novo.
            BaseIcms = !string.IsNullOrWhiteSpace(cstIcmsResolvido) && CalculoFiscalHelper.CstIcmsTributacaoIntegral.Contains(cstIcmsResolvido)
                ? Math.Round(quantidade * precoUnitario, 2, MidpointRounding.AwayFromZero)
                : null,
            CstPis = tributacao?.CstPis,
            AliquotaPis = tributacao?.AliquotaPis,
            CstCofins = tributacao?.CstCofins,
            AliquotaCofins = tributacao?.AliquotaCofins,
            CstIpi = tributacao?.CstIpi,
            AliquotaIpi = tributacao?.AliquotaIpi,
            CodigoEnquadramentoIpi = tributacao?.CodigoEnquadramentoIpi,
            CstIbsCbs = tributacao?.CstIbsCbs ?? empresa.CstIbsCbsPadrao ?? "000",
            CClassTrib = tributacao?.CClassTrib ?? empresa.CClassTribPadrao ?? "000001",
            UnidadeTributavel = tributacao?.UnidadeTributavel
        };

        // Base ad-valorem do IPI = valor do item, mesma convenção já usada para ICMS/IBS/CBS
        // (BaseIcms/BaseIbsCbs) — este módulo não modela cenários de IPI por valor fixo
        // (Produto.ValorIpiFixo) nem por unidade (qUnid/vUnid), só o caso comum.
        item.BaseIpi = item.ValorTotal;

        var resultadoCalculo = CalculoFiscalHelper.CalcularItem(
            produto.Id, produto.Nome, item.ValorTotal,
            item.CstIcms, item.CsosnIcms, item.AliquotaIcms,
            item.CstPis, item.AliquotaPis,
            item.CstCofins, item.AliquotaCofins,
            item.CstIpi, item.AliquotaIpi,
            empresa.AliquotaIbsUfPadrao ?? 0m, empresa.AliquotaIbsMunicipioPadrao ?? 0m, empresa.AliquotaCbsPadrao ?? 0m);

        item.ValorIcms = resultadoCalculo.VIcms;
        item.ValorPis = resultadoCalculo.VPis;
        item.ValorCofins = resultadoCalculo.VCofins;
        item.ValorIpi = resultadoCalculo.VIpi;
        item.BaseIbsCbs = item.ValorTotal;
        item.AliquotaIbsUf = empresa.AliquotaIbsUfPadrao;
        item.ValorIbsUf = resultadoCalculo.VIbsUf;
        item.AliquotaIbsMunicipio = empresa.AliquotaIbsMunicipioPadrao;
        item.ValorIbsMunicipio = resultadoCalculo.VIbsMunicipio;
        item.AliquotaCbs = empresa.AliquotaCbsPadrao;
        item.ValorCbs = resultadoCalculo.VCbs;
        item.Avisos = resultadoCalculo.Avisos;

        return item;
    }

    public NotaFiscalDto RecalcularTotais(NotaFiscalDto nota)
    {
        if (!nota.CalculoAutomatico)
            return nota;

        for (var i = 0; i < nota.Itens.Count; i++)
            nota.Itens[i].NItem = i + 1;

        nota.VProd = nota.Itens.Where(i => i.CompoeTotalNota).Sum(i => i.ValorTotal);
        nota.VBcIcms = nota.Itens.Where(i => i.ValorIcms is > 0).Sum(i => i.BaseIcms ?? 0);
        nota.VIcms = nota.Itens.Sum(i => i.ValorIcms ?? 0);
        nota.VBcIcmsSt = nota.Itens.Sum(i => i.BaseIcmsSt ?? 0);
        nota.VIcmsSt = nota.Itens.Sum(i => i.ValorIcmsSt ?? 0);
        nota.VIpi = nota.Itens.Sum(i => i.ValorIpi ?? 0);
        nota.VBcIbsCbs = nota.Itens.Sum(i => i.BaseIbsCbs ?? 0);
        nota.VIbsUf = nota.Itens.Sum(i => i.ValorIbsUf ?? 0);
        nota.VIbsMunicipio = nota.Itens.Sum(i => i.ValorIbsMunicipio ?? 0);
        nota.VIbs = nota.VIbsUf + nota.VIbsMunicipio;
        nota.VCbs = nota.Itens.Sum(i => i.ValorCbs ?? 0);
        nota.NumeroItens = nota.Itens.Count;
        // Fórmula oficial (GUIA_INTEGRACAO.md seção 4.8): vNF = vProd + vST + vFrete + vSeg +
        // vOutro + vIPI − vDesc (vICMSDeson não é modelado neste domínio). Faltar o vST aqui
        // subestimava nota.VNf sempre que havia ICMS-ST — o XML transmitido já estava certo
        // (NotaFiscalPayloadBuilder.ConstruirTotal recalcula do zero a partir dos itens), mas
        // o valor persistido/exibido em listagens e usado por NotaFiscalValidator para
        // conferir "soma dos pagamentos bate com o total" ficava errado.
        nota.VNf = nota.VProd + nota.VIcmsSt + nota.VServ + nota.VFrete + nota.VSeg + nota.VOutro + nota.VIpi - nota.VDesc;

        return nota;
    }

    public async Task<NotaFiscalDto> CriarRascunhoAsync(NotaFiscalDto nota, CancellationToken cancellationToken = default)
    {
        var entidade = MapearParaEntidade(nota, new NotaFiscal());
        entidade.Status = StatusNotaFiscal.Rascunho;
        var criada = await _notaFiscalRepository.CriarAsync(entidade, cancellationToken);
        return await MapearParaDtoAsync(criada);
    }

    public async Task<NotaFiscalDto> AtualizarRascunhoAsync(NotaFiscalDto nota, CancellationToken cancellationToken = default)
    {
        var existente = await _notaFiscalRepository.ObterPorIdAsync(nota.Id, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {nota.Id} não encontrada.");

        if (existente.Status is not (StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada))
            throw new DomainException("Só é possível editar notas em Rascunho ou Rejeitadas.");

        var entidade = MapearParaEntidade(nota, existente);
        entidade.Status = StatusNotaFiscal.Rascunho;
        await _notaFiscalRepository.AtualizarAsync(entidade, substituirItens: true, cancellationToken);
        return await ObterPorIdAsync(nota.Id, cancellationToken) ?? nota;
    }

    public async Task<NotaFiscalDto?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var nota = await _notaFiscalRepository.ObterPorIdAsync(id, cancellationToken);
        return nota is null ? null : await MapearParaDtoAsync(nota);
    }

    public async Task ExcluirAsync(int notaFiscalId, CancellationToken cancellationToken = default)
    {
        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (entidade.Status is not (StatusNotaFiscal.Rascunho or StatusNotaFiscal.Rejeitada))
            throw new DomainException(
                "Só é possível excluir notas em Rascunho ou Rejeitadas — uma nota Autorizada/Cancelada é documento fiscal e não pode ser apagada localmente.");

        await _notaFiscalRepository.ExcluirAsync(notaFiscalId, cancellationToken);
    }

    public async Task<IReadOnlyList<NotaFiscalResumoDto>> ListarAsync(
        TipoNotaFiscal tipo, StatusNotaFiscal? status = null, CancellationToken cancellationToken = default)
    {
        var notas = await _notaFiscalRepository.ListarAsync(tipo, status, cancellationToken);
        return notas.Select(n => new NotaFiscalResumoDto
        {
            Id = n.Id,
            Tipo = n.Tipo,
            Serie = n.Serie,
            Numero = n.Numero,
            DataEmissao = n.DataEmissao,
            Status = n.Status,
            ClienteNome = n.Cliente?.Nome ?? n.DestinatarioNome,
            VNf = n.VNf,
            ChaveAcesso = n.ChaveAcesso
        }).ToList();
    }

    public async Task<ResumoNotasFiscaisDto> ObterResumoAsync(CancellationToken cancellationToken = default)
    {
        var (emitidas, canceladas, valorTotalEmitido) = await _notaFiscalRepository.ObterContadoresAsync(cancellationToken);
        var ultimas = await _notaFiscalRepository.ListarUltimasAsync(10, cancellationToken);

        return new ResumoNotasFiscaisDto
        {
            TotalEmitidas = emitidas,
            TotalCanceladas = canceladas,
            ValorTotalEmitido = valorTotalEmitido,
            UltimasNotas = ultimas.Select(n => new NotaFiscalResumoDto
            {
                Id = n.Id,
                Tipo = n.Tipo,
                Serie = n.Serie,
                Numero = n.Numero,
                DataEmissao = n.DataEmissao,
                Status = n.Status,
                ClienteNome = n.Cliente?.Nome ?? n.DestinatarioNome,
                VNf = n.VNf,
                ChaveAcesso = n.ChaveAcesso
            }).ToList()
        };
    }

    public async Task<string> ObterProximoNumeroAsync(TipoNotaFiscal tipo, string? serie = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serie))
        {
            var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
            serie = string.IsNullOrWhiteSpace(empresa.Serie) ? "1" : empresa.Serie;
        }

        return await _notaFiscalRepository.ObterProximoNumeroAsync(tipo, serie, cancellationToken);
    }

    public async Task<NotaFiscalDto> EmitirAsync(int notaFiscalId, CancellationToken cancellationToken = default)
    {
        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (entidade.Status == StatusNotaFiscal.Autorizada)
            throw new DomainException("Esta nota já está autorizada.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(empresa.ApiKeyFiscal))
            throw new DomainException("Cadastre a API Key da API Fiscal em Configurações → Fiscal antes de emitir.");

        var dtoValidacao = await MapearParaDtoAsync(entidade);
        NotaFiscalValidator.ValidarParaEmissao(dtoValidacao, empresa.Uf ?? string.Empty, empresa.ValidarNcmEmNotas);

        // dhEmi tem tolerância de horário na SEFAZ (cStat 228 "Data de emissão muito
        // atrasada") — a nota pode ter sido digitada/salva como rascunho horas ou dias
        // antes deste clique em "Emitir" (Ações da Nota, de propósito separado da tela de
        // cadastro). O horário enviado precisa ser o momento real da transmissão, não o
        // da digitação, então é atualizado aqui, imediatamente antes de montar o payload.
        entidade.DataEmissao = DateTime.Now;

        if (string.IsNullOrWhiteSpace(empresa.Cnpj) || string.IsNullOrWhiteSpace(empresa.RazaoSocial) ||
            string.IsNullOrWhiteSpace(empresa.InscricaoEstadual))
            throw new DomainException("Cadastre o Emitente (CNPJ, Razão Social e Inscrição Estadual) em Configurações → Fiscal → Emitente antes de emitir.");

        // enderEmit é obrigatório e sem fallback no layout oficial da NF-e — faltando
        // qualquer um desses campos, a API monta um XML inválido e pode devolver um erro
        // 500 genérico em vez de uma rejeição de validação clara (dependendo de como o
        // schema é processado do lado de lá). Falhar aqui, cedo e com mensagem específica,
        // é melhor do que deixar isso virar um "Erro interno" opaco vindo da API Fiscal.
        var camposEnderecoFaltando = new List<string>();
        if (string.IsNullOrWhiteSpace(empresa.Logradouro)) camposEnderecoFaltando.Add("Logradouro");
        if (string.IsNullOrWhiteSpace(empresa.Bairro)) camposEnderecoFaltando.Add("Bairro");
        if (string.IsNullOrWhiteSpace(empresa.CodigoMunicipioIbge)) camposEnderecoFaltando.Add("Código IBGE do Município");
        if (string.IsNullOrWhiteSpace(empresa.NomeMunicipio)) camposEnderecoFaltando.Add("Município");
        if (string.IsNullOrWhiteSpace(empresa.Uf)) camposEnderecoFaltando.Add("UF");
        if (string.IsNullOrWhiteSpace(empresa.Cep)) camposEnderecoFaltando.Add("CEP");
        if (camposEnderecoFaltando.Count > 0)
            throw new DomainException(
                $"Complete o endereço fiscal do emitente em Configurações → Fiscal → Emitente antes de emitir. Faltando: {string.Join(", ", camposEnderecoFaltando)}.");

        var emitente = new EmitenteFiscalDto
        {
            Cnpj = empresa.Cnpj,
            RazaoSocial = empresa.RazaoSocial,
            NomeFantasia = empresa.NomeFantasia,
            InscricaoEstadual = empresa.InscricaoEstadual,
            Crt = entidade.Crt,
            Logradouro = empresa.Logradouro ?? string.Empty,
            Numero = empresa.Numero ?? "S/N",
            Complemento = empresa.Complemento,
            Bairro = empresa.Bairro ?? string.Empty,
            CodigoMunicipioIbge = empresa.CodigoMunicipioIbge ?? string.Empty,
            Municipio = empresa.NomeMunicipio ?? string.Empty,
            Uf = empresa.Uf ?? string.Empty,
            Cep = empresa.Cep ?? string.Empty
        };

        var (cscId, cscSecret) = ObterCscDoAmbiente(empresa, entidade.Ambiente);

        ResultadoEmissaoFiscalDto resultado;
        try
        {
            resultado = await _fiscalApiClient.EmitirAsync(entidade, emitente, empresa.ApiKeyFiscal, cscId, cscSecret, cancellationToken);
        }
        catch (FiscalApiException ex)
        {
            // 502/503/504: não dá pra saber se a SEFAZ autorizou ou não (seção 9.5 do
            // guia) — fica Indeterminada, o operador deve consultar status antes de
            // reemitir. Qualquer outro erro (400/401/422) é rejeição determinística: a
            // nota não foi transmitida, é seguro corrigir e tentar de novo.
            entidade.Status = ex.ConsultarStatusAntesDeReemitir ? StatusNotaFiscal.Indeterminada : StatusNotaFiscal.Rejeitada;
            entidade.MensagemErro = ex.Message;
            entidade.TraceId = ex.TraceId;
            await _notaFiscalRepository.AtualizarAsync(entidade, cancellationToken: cancellationToken);
            await RegistrarEventoAsync(entidade.Id, TipoEventoNotaFiscal.Emissao, sucesso: false,
                cStat: ex.HttpStatus?.ToString(), xMotivo: ex.Message, traceId: ex.TraceId, cancellationToken: cancellationToken);
            throw;
        }

        entidade.Status = resultado.Aprovado ? StatusNotaFiscal.Autorizada : StatusNotaFiscal.Rejeitada;
        entidade.ChaveAcesso = resultado.ChaveAcesso ?? entidade.ChaveAcesso;
        entidade.NProt = resultado.NProt;
        entidade.DhRecbto = resultado.DhRecbto;
        entidade.CStat = resultado.CStat;
        entidade.XMotivo = resultado.XMotivo;
        entidade.XmlAutorizado = resultado.XmlAutorizado;
        entidade.QrCodeUrl = resultado.QrCodeUrl;
        entidade.MensagemErro = resultado.Aprovado ? null : (resultado.XMotivo ?? resultado.Erro);

        await _notaFiscalRepository.AtualizarAsync(entidade, cancellationToken: cancellationToken);
        await RegistrarEventoAsync(entidade.Id, TipoEventoNotaFiscal.Emissao, resultado.Aprovado,
            nProtEvento: resultado.NProt, cStat: resultado.CStat, xMotivo: resultado.XMotivo, cancellationToken: cancellationToken);

        return await ObterPorIdAsync(notaFiscalId, cancellationToken) ?? dtoValidacao;
    }

    public async Task<NotaFiscalDto> CancelarAsync(int notaFiscalId, string justificativa, CancellationToken cancellationToken = default)
    {
        NotaFiscalValidator.ValidarJustificativaCancelamento(justificativa);

        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (entidade.Status != StatusNotaFiscal.Autorizada)
            throw new DomainException("Só é possível cancelar uma nota autorizada.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var resultado = await _fiscalApiClient.CancelarAsync(entidade, empresa.Cnpj ?? string.Empty, justificativa, empresa.ApiKeyFiscal ?? string.Empty, cancellationToken);

        if (resultado.Aprovado)
        {
            entidade.Status = StatusNotaFiscal.Cancelada;
            await _notaFiscalRepository.AtualizarAsync(entidade, cancellationToken: cancellationToken);
        }

        await RegistrarEventoAsync(entidade.Id, TipoEventoNotaFiscal.Cancelamento, resultado.Aprovado,
            nProtEvento: resultado.NProt, cStat: resultado.CStat, xMotivo: resultado.XMotivo, texto: justificativa,
            cancellationToken: cancellationToken);

        if (!resultado.Aprovado)
            throw new DomainException(resultado.XMotivo ?? resultado.Erro ?? "Cancelamento rejeitado pela SEFAZ.");

        return await ObterPorIdAsync(notaFiscalId, cancellationToken) ?? throw new DomainException("Falha ao recarregar a nota após o cancelamento.");
    }

    public async Task<NotaFiscalDto> CartaCorrecaoAsync(int notaFiscalId, string correcao, CancellationToken cancellationToken = default)
    {
        NotaFiscalValidator.ValidarCorrecao(correcao);

        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (entidade.Tipo != TipoNotaFiscal.NFe)
            throw new DomainException("Carta de correção é exclusiva de NF-e.");
        if (entidade.Status != StatusNotaFiscal.Autorizada)
            throw new DomainException("Só é possível corrigir uma nota autorizada.");

        var sequencial = entidade.Eventos.Count(e => e.Tipo == TipoEventoNotaFiscal.CartaCorrecao) + 1;
        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var resultado = await _fiscalApiClient.CartaCorrecaoAsync(
            entidade, empresa.Cnpj ?? string.Empty, correcao, sequencial, empresa.ApiKeyFiscal ?? string.Empty, cancellationToken);

        await RegistrarEventoAsync(entidade.Id, TipoEventoNotaFiscal.CartaCorrecao, resultado.Aprovado,
            nProtEvento: resultado.NProt, cStat: resultado.CStat, xMotivo: resultado.XMotivo, texto: correcao,
            sequencial: sequencial, cancellationToken: cancellationToken);

        if (!resultado.Aprovado)
            throw new DomainException(resultado.XMotivo ?? resultado.Erro ?? "Carta de correção rejeitada pela SEFAZ.");

        return await ObterPorIdAsync(notaFiscalId, cancellationToken) ?? throw new DomainException("Falha ao recarregar a nota após a CC-e.");
    }

    public async Task<ResultadoInutilizacaoFiscalDto> InutilizarAsync(
        string cUF, string ano, string serie, string numeroInicial, string numeroFinal, string justificativa,
        AmbienteEmissaoFiscal ambiente, CancellationToken cancellationToken = default)
    {
        NotaFiscalValidator.ValidarInutilizacao(justificativa, numeroInicial, numeroFinal);

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(empresa.ApiKeyFiscal))
            throw new DomainException("Cadastre a API Key da API Fiscal em Configurações → Fiscal antes de inutilizar.");

        return await _fiscalApiClient.InutilizarAsync(
            empresa.Cnpj ?? string.Empty, cUF, ano, serie, numeroInicial, numeroFinal, justificativa, ambiente, empresa.ApiKeyFiscal, cancellationToken);
    }

    public async Task<NotaFiscalDto> ConsultarStatusAsync(int notaFiscalId, CancellationToken cancellationToken = default)
    {
        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (string.IsNullOrWhiteSpace(entidade.ChaveAcesso))
            throw new DomainException("Esta nota ainda não foi emitida — não há chave de acesso para consultar.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        var resultado = await _fiscalApiClient.ConsultarStatusAsync(
            entidade.Tipo, entidade.ChaveAcesso, entidade.Ambiente, empresa.ApiKeyFiscal ?? string.Empty, cancellationToken);

        entidade.CStat = resultado.CStat ?? entidade.CStat;
        entidade.XMotivo = resultado.XMotivo ?? entidade.XMotivo;
        entidade.Status = resultado.Situacao switch
        {
            "Autorizada" => StatusNotaFiscal.Autorizada,
            "Cancelada" => StatusNotaFiscal.Cancelada,
            "Denegada" => StatusNotaFiscal.Denegada,
            "Rejeitada" => StatusNotaFiscal.Rejeitada,
            "Indeterminada" => StatusNotaFiscal.Indeterminada,
            _ => entidade.Status
        };

        await _notaFiscalRepository.AtualizarAsync(entidade, cancellationToken: cancellationToken);
        await RegistrarEventoAsync(entidade.Id, TipoEventoNotaFiscal.ConsultaStatus, resultado.ConfirmadoNaSefaz,
            cStat: resultado.CStat, xMotivo: resultado.XMotivo, cancellationToken: cancellationToken);

        return await ObterPorIdAsync(notaFiscalId, cancellationToken) ?? throw new DomainException("Falha ao recarregar a nota após a consulta.");
    }

    public async Task<string> ObterXmlAsync(int notaFiscalId, CancellationToken cancellationToken = default)
    {
        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (!string.IsNullOrWhiteSpace(entidade.XmlAutorizado))
            return entidade.XmlAutorizado;

        if (string.IsNullOrWhiteSpace(entidade.ChaveAcesso))
            throw new DomainException("Esta nota ainda não foi emitida.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        return await _fiscalApiClient.ObterXmlAsync(entidade.Tipo, entidade.ChaveAcesso, entidade.Ambiente, empresa.ApiKeyFiscal ?? string.Empty, cancellationToken);
    }

    public async Task<byte[]> ObterDanfeAsync(int notaFiscalId, CancellationToken cancellationToken = default)
    {
        var entidade = await _notaFiscalRepository.ObterPorIdAsync(notaFiscalId, cancellationToken)
            ?? throw new DomainException($"Nota fiscal com Id {notaFiscalId} não encontrada.");

        if (string.IsNullOrWhiteSpace(entidade.ChaveAcesso))
            throw new DomainException("Esta nota ainda não foi emitida.");

        var empresa = await _configuracaoFiscal.ObterConfiguracaoEmpresaAsync(cancellationToken);
        return await _fiscalApiClient.ObterDanfeAsync(entidade.Tipo, entidade.ChaveAcesso, entidade.Ambiente, empresa.ApiKeyFiscal ?? string.Empty, cancellationToken);
    }

    // --- Helpers privados ---

    private static (string? CscId, string? CscSecret) ObterCscDoAmbiente(ConfiguracaoFiscalEmpresaDto empresa, AmbienteEmissaoFiscal ambiente) =>
        ambiente == AmbienteEmissaoFiscal.Producao
            ? (empresa.IdCscProducao, empresa.CscProducao)
            : (empresa.IdCscHomologacao, empresa.CscHomologacao);

    private async Task RegistrarEventoAsync(
        int notaFiscalId, TipoEventoNotaFiscal tipo, bool sucesso,
        string? nProtEvento = null, string? cStat = null, string? xMotivo = null, string? texto = null,
        int? sequencial = null, string? traceId = null, CancellationToken cancellationToken = default)
    {
        await _notaFiscalRepository.AdicionarEventoAsync(new NotaFiscalEvento
        {
            NotaFiscalId = notaFiscalId,
            Tipo = tipo,
            DataHora = DateTime.Now,
            Sucesso = sucesso,
            NProtEvento = nProtEvento,
            CStat = cStat,
            XMotivo = xMotivo,
            Texto = texto,
            Sequencial = sequencial,
            TraceId = traceId
        }, cancellationToken);
    }

    private static void PreencherDestinatarioDeCliente(NotaFiscalDto nota, Cliente cliente)
    {
        nota.DestinatarioNome = cliente.Nome;
        nota.DestinatarioTipoPessoa = cliente.TipoPessoa;
        nota.DestinatarioDocumento = cliente.TipoPessoa == TipoPessoa.Juridica ? cliente.Cnpj : cliente.Cpf;
        nota.DestinatarioIndicadorIe = cliente.IndicadorIe;
        nota.DestinatarioInscricaoEstadual = cliente.InscricaoEstadual;
        nota.DestinatarioEmail = cliente.Email;
        nota.DestinatarioTelefone = cliente.Telefone;
        nota.DestinatarioCep = cliente.Cep;
        nota.DestinatarioLogradouro = cliente.Logradouro;
        nota.DestinatarioNumero = cliente.Numero;
        nota.DestinatarioComplemento = cliente.Complemento;
        nota.DestinatarioBairro = cliente.Bairro;
        nota.DestinatarioCidade = cliente.Cidade;
        nota.DestinatarioUf = cliente.Estado;
        nota.DestinatarioCodigoMunicipioIbge = cliente.CodigoMunicipioIbge;
    }

    private static TributacaoProduto MapearTributacaoCategoriaComoProduto(TributacaoCategoria c, int produtoId) => new()
    {
        ProdutoId = produtoId,
        Ncm = c.Ncm,
        Cest = c.Cest,
        Origem = c.Origem,
        CstIcms = c.CstIcms,
        CsosnIcms = c.CsosnIcms,
        AliquotaIcms = c.AliquotaIcms,
        CstPis = c.CstPis,
        AliquotaPis = c.AliquotaPis,
        CstCofins = c.CstCofins,
        AliquotaCofins = c.AliquotaCofins,
        CfopDentroEstado = c.CfopDentroEstado,
        CfopForaEstado = c.CfopForaEstado,
        CstIbsCbs = c.CstIbsCbs,
        CClassTrib = c.CClassTrib
    };

    private static NotaFiscal MapearParaEntidade(NotaFiscalDto dto, NotaFiscal entidade)
    {
        entidade.Id = dto.Id;
        entidade.Tipo = dto.Tipo;
        entidade.VendaId = dto.VendaId;
        entidade.ClienteId = dto.ClienteId;
        entidade.NaturezaOperacaoId = dto.NaturezaOperacaoId;
        entidade.TipoSaida = dto.TipoSaida;
        entidade.Serie = dto.Serie;
        entidade.Numero = dto.Numero;
        entidade.DataEmissao = dto.DataEmissao;
        entidade.DataSaida = dto.DataSaida;
        entidade.NaturezaOperacaoDescricao = dto.NaturezaOperacaoDescricao;
        entidade.Finalidade = dto.Finalidade;
        entidade.ConsumidorFinal = dto.ConsumidorFinal;
        entidade.IndicadorPresenca = dto.IndicadorPresenca;
        entidade.IntermediadorCnpj = dto.IntermediadorCnpj;
        entidade.IntermediadorIdentificador = dto.IntermediadorIdentificador;
        entidade.Crt = dto.Crt;
        entidade.Ambiente = dto.Ambiente;

        entidade.DestinatarioNome = dto.DestinatarioNome;
        entidade.DestinatarioTipoPessoa = dto.DestinatarioTipoPessoa;
        entidade.DestinatarioDocumento = dto.DestinatarioDocumento;
        entidade.DestinatarioIndicadorIe = dto.DestinatarioIndicadorIe;
        entidade.DestinatarioInscricaoEstadual = dto.DestinatarioInscricaoEstadual;
        entidade.DestinatarioEmail = dto.DestinatarioEmail;
        entidade.DestinatarioTelefone = dto.DestinatarioTelefone;
        entidade.DestinatarioCep = dto.DestinatarioCep;
        entidade.DestinatarioLogradouro = dto.DestinatarioLogradouro;
        entidade.DestinatarioNumero = dto.DestinatarioNumero;
        entidade.DestinatarioComplemento = dto.DestinatarioComplemento;
        entidade.DestinatarioBairro = dto.DestinatarioBairro;
        entidade.DestinatarioCidade = dto.DestinatarioCidade;
        entidade.DestinatarioUf = dto.DestinatarioUf;
        entidade.DestinatarioCodigoMunicipioIbge = dto.DestinatarioCodigoMunicipioIbge;
        entidade.Suframa = dto.Suframa;
        entidade.Vendedor = dto.Vendedor;
        entidade.ListaPrecoNome = dto.ListaPrecoNome;

        entidade.EntregaDiferenteCobranca = dto.EntregaDiferenteCobranca;
        entidade.EntregaCep = dto.EntregaCep;
        entidade.EntregaLogradouro = dto.EntregaLogradouro;
        entidade.EntregaNumero = dto.EntregaNumero;
        entidade.EntregaComplemento = dto.EntregaComplemento;
        entidade.EntregaBairro = dto.EntregaBairro;
        entidade.EntregaCidade = dto.EntregaCidade;
        entidade.EntregaUf = dto.EntregaUf;
        entidade.EntregaCodigoMunicipioIbge = dto.EntregaCodigoMunicipioIbge;

        entidade.CalculoAutomatico = dto.CalculoAutomatico;
        entidade.VProd = dto.VProd;
        entidade.VServ = dto.VServ;
        entidade.VFrete = dto.VFrete;
        entidade.VSeg = dto.VSeg;
        entidade.VBcIcms = dto.VBcIcms;
        entidade.VIcms = dto.VIcms;
        entidade.VBcIcmsSt = dto.VBcIcmsSt;
        entidade.VIcmsSt = dto.VIcmsSt;
        entidade.VIpi = dto.VIpi;
        entidade.VIpiDevolvido = dto.VIpiDevolvido;
        entidade.VIssqn = dto.VIssqn;
        entidade.VOutro = dto.VOutro;
        entidade.VDesc = dto.VDesc;
        entidade.VFunrural = dto.VFunrural;
        entidade.NumeroItens = dto.NumeroItens;
        entidade.VAproxImp = dto.VAproxImp;
        entidade.VFcp = dto.VFcp;
        entidade.VFcpSt = dto.VFcpSt;
        entidade.VFcpStRet = dto.VFcpStRet;
        entidade.VBcIbsCbs = dto.VBcIbsCbs;
        entidade.VIbsUf = dto.VIbsUf;
        entidade.VIbsMunicipio = dto.VIbsMunicipio;
        entidade.VIbs = dto.VIbs;
        entidade.VCbs = dto.VCbs;
        entidade.VNf = dto.VNf;

        entidade.FormaEnvio = dto.FormaEnvio;
        entidade.PesoBruto = dto.PesoBruto;
        entidade.PesoLiquido = dto.PesoLiquido;
        entidade.EnviarParaExpedicao = dto.EnviarParaExpedicao;

        entidade.FormaRecebimento = dto.FormaRecebimento;
        entidade.CategoriaFinanceira = dto.CategoriaFinanceira;
        entidade.CondicaoPagamento = dto.CondicaoPagamento;

        entidade.Deposito = dto.Deposito;
        entidade.Observacoes = dto.Observacoes;
        entidade.ObservacoesSistema = dto.ObservacoesSistema;
        entidade.InformacoesFisco = dto.InformacoesFisco;
        entidade.Marcadores = dto.Marcadores;

        entidade.Itens = dto.Itens.Select(MapearItemParaEntidade).ToList();
        entidade.Pagamentos = dto.Pagamentos.Select(MapearPagamentoParaEntidade).ToList();

        return entidade;
    }

    private static ItemNotaFiscal MapearItemParaEntidade(ItemNotaFiscalDto dto) => new()
    {
        ProdutoId = dto.ProdutoId,
        NItem = dto.NItem,
        CodigoProduto = dto.CodigoProduto,
        CodigoBarras = dto.CodigoBarras,
        Descricao = dto.Descricao,
        Ncm = dto.Ncm,
        Cest = dto.Cest,
        Cfop = dto.Cfop,
        Unidade = dto.Unidade,
        Quantidade = dto.Quantidade,
        ValorUnitario = dto.ValorUnitario,
        ValorTotal = dto.ValorTotal,
        UnidadeTributavel = dto.UnidadeTributavel,
        QuantidadeTributavel = dto.QuantidadeTributavel,
        ValorUnitarioTributavel = dto.ValorUnitarioTributavel,
        CompoeTotalNota = dto.CompoeTotalNota,
        Origem = dto.Origem,
        CstIcms = dto.CstIcms,
        CsosnIcms = dto.CsosnIcms,
        BaseIcms = dto.BaseIcms,
        AliquotaIcms = dto.AliquotaIcms,
        ValorIcms = dto.ValorIcms,
        BaseIcmsSt = dto.BaseIcmsSt,
        AliquotaIcmsSt = dto.AliquotaIcmsSt,
        ValorIcmsSt = dto.ValorIcmsSt,
        CstPis = dto.CstPis,
        BasePis = dto.BasePis,
        AliquotaPis = dto.AliquotaPis,
        ValorPis = dto.ValorPis,
        CstCofins = dto.CstCofins,
        BaseCofins = dto.BaseCofins,
        AliquotaCofins = dto.AliquotaCofins,
        ValorCofins = dto.ValorCofins,
        CstIpi = dto.CstIpi,
        BaseIpi = dto.BaseIpi,
        AliquotaIpi = dto.AliquotaIpi,
        ValorIpi = dto.ValorIpi,
        CodigoEnquadramentoIpi = dto.CodigoEnquadramentoIpi,
        CstIbsCbs = dto.CstIbsCbs,
        CClassTrib = dto.CClassTrib,
        BaseIbsCbs = dto.BaseIbsCbs,
        AliquotaIbsUf = dto.AliquotaIbsUf,
        ValorIbsUf = dto.ValorIbsUf,
        AliquotaIbsMunicipio = dto.AliquotaIbsMunicipio,
        ValorIbsMunicipio = dto.ValorIbsMunicipio,
        AliquotaCbs = dto.AliquotaCbs,
        ValorCbs = dto.ValorCbs
    };

    private static NotaFiscalPagamento MapearPagamentoParaEntidade(NotaFiscalPagamentoDto dto) => new()
    {
        FormaPagamento = dto.FormaPagamento,
        Valor = dto.Valor,
        QuantidadeParcelas = dto.QuantidadeParcelas,
        Ordem = dto.Ordem
    };

    private async Task<NotaFiscalDto> MapearParaDtoAsync(NotaFiscal n)
    {
        string? clienteNome = n.Cliente?.Nome;
        if (clienteNome is null && n.ClienteId.HasValue)
            clienteNome = (await _clienteRepository.ObterPorIdAsync(n.ClienteId.Value))?.Nome;

        return new NotaFiscalDto
        {
            Id = n.Id,
            Tipo = n.Tipo,
            VendaId = n.VendaId,
            ClienteId = n.ClienteId,
            ClienteNome = clienteNome,
            NaturezaOperacaoId = n.NaturezaOperacaoId,
            TipoSaida = n.TipoSaida,
            Serie = n.Serie,
            Numero = n.Numero,
            DataEmissao = n.DataEmissao,
            DataSaida = n.DataSaida,
            NaturezaOperacaoDescricao = n.NaturezaOperacaoDescricao,
            Finalidade = n.Finalidade,
            ConsumidorFinal = n.ConsumidorFinal,
            IndicadorPresenca = n.IndicadorPresenca,
            IntermediadorCnpj = n.IntermediadorCnpj,
            IntermediadorIdentificador = n.IntermediadorIdentificador,
            Crt = n.Crt,
            Ambiente = n.Ambiente,

            DestinatarioNome = n.DestinatarioNome,
            DestinatarioTipoPessoa = n.DestinatarioTipoPessoa,
            DestinatarioDocumento = n.DestinatarioDocumento,
            DestinatarioIndicadorIe = n.DestinatarioIndicadorIe,
            DestinatarioInscricaoEstadual = n.DestinatarioInscricaoEstadual,
            DestinatarioEmail = n.DestinatarioEmail,
            DestinatarioTelefone = n.DestinatarioTelefone,
            DestinatarioCep = n.DestinatarioCep,
            DestinatarioLogradouro = n.DestinatarioLogradouro,
            DestinatarioNumero = n.DestinatarioNumero,
            DestinatarioComplemento = n.DestinatarioComplemento,
            DestinatarioBairro = n.DestinatarioBairro,
            DestinatarioCidade = n.DestinatarioCidade,
            DestinatarioUf = n.DestinatarioUf,
            DestinatarioCodigoMunicipioIbge = n.DestinatarioCodigoMunicipioIbge,
            Suframa = n.Suframa,
            Vendedor = n.Vendedor,
            ListaPrecoNome = n.ListaPrecoNome,

            EntregaDiferenteCobranca = n.EntregaDiferenteCobranca,
            EntregaCep = n.EntregaCep,
            EntregaLogradouro = n.EntregaLogradouro,
            EntregaNumero = n.EntregaNumero,
            EntregaComplemento = n.EntregaComplemento,
            EntregaBairro = n.EntregaBairro,
            EntregaCidade = n.EntregaCidade,
            EntregaUf = n.EntregaUf,
            EntregaCodigoMunicipioIbge = n.EntregaCodigoMunicipioIbge,

            CalculoAutomatico = n.CalculoAutomatico,
            VProd = n.VProd,
            VServ = n.VServ,
            VFrete = n.VFrete,
            VSeg = n.VSeg,
            VBcIcms = n.VBcIcms,
            VIcms = n.VIcms,
            VBcIcmsSt = n.VBcIcmsSt,
            VIcmsSt = n.VIcmsSt,
            VIpi = n.VIpi,
            VIpiDevolvido = n.VIpiDevolvido,
            VIssqn = n.VIssqn,
            VOutro = n.VOutro,
            VDesc = n.VDesc,
            VFunrural = n.VFunrural,
            NumeroItens = n.NumeroItens,
            VAproxImp = n.VAproxImp,
            VFcp = n.VFcp,
            VFcpSt = n.VFcpSt,
            VFcpStRet = n.VFcpStRet,
            VBcIbsCbs = n.VBcIbsCbs,
            VIbsUf = n.VIbsUf,
            VIbsMunicipio = n.VIbsMunicipio,
            VIbs = n.VIbs,
            VCbs = n.VCbs,
            VNf = n.VNf,

            FormaEnvio = n.FormaEnvio,
            PesoBruto = n.PesoBruto,
            PesoLiquido = n.PesoLiquido,
            EnviarParaExpedicao = n.EnviarParaExpedicao,

            FormaRecebimento = n.FormaRecebimento,
            CategoriaFinanceira = n.CategoriaFinanceira,
            CondicaoPagamento = n.CondicaoPagamento,

            Deposito = n.Deposito,
            Observacoes = n.Observacoes,
            ObservacoesSistema = n.ObservacoesSistema,
            InformacoesFisco = n.InformacoesFisco,
            Marcadores = n.Marcadores,

            Status = n.Status,
            ChaveAcesso = n.ChaveAcesso,
            NProt = n.NProt,
            DhRecbto = n.DhRecbto,
            CStat = n.CStat,
            XMotivo = n.XMotivo,
            QrCodeUrl = n.QrCodeUrl,
            MensagemErro = n.MensagemErro,

            Itens = n.Itens.OrderBy(i => i.NItem).Select(i => new ItemNotaFiscalDto
            {
                Id = i.Id,
                ProdutoId = i.ProdutoId,
                NItem = i.NItem,
                CodigoProduto = i.CodigoProduto,
                CodigoBarras = i.CodigoBarras,
                Descricao = i.Descricao,
                Ncm = i.Ncm,
                Cest = i.Cest,
                Cfop = i.Cfop,
                Unidade = i.Unidade,
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario,
                ValorTotal = i.ValorTotal,
                UnidadeTributavel = i.UnidadeTributavel,
                QuantidadeTributavel = i.QuantidadeTributavel,
                ValorUnitarioTributavel = i.ValorUnitarioTributavel,
                CompoeTotalNota = i.CompoeTotalNota,
                Origem = i.Origem,
                CstIcms = i.CstIcms,
                CsosnIcms = i.CsosnIcms,
                BaseIcms = i.BaseIcms,
                AliquotaIcms = i.AliquotaIcms,
                ValorIcms = i.ValorIcms,
                BaseIcmsSt = i.BaseIcmsSt,
                AliquotaIcmsSt = i.AliquotaIcmsSt,
                ValorIcmsSt = i.ValorIcmsSt,
                CstPis = i.CstPis,
                BasePis = i.BasePis,
                AliquotaPis = i.AliquotaPis,
                ValorPis = i.ValorPis,
                CstCofins = i.CstCofins,
                BaseCofins = i.BaseCofins,
                AliquotaCofins = i.AliquotaCofins,
                ValorCofins = i.ValorCofins,
                CstIpi = i.CstIpi,
                BaseIpi = i.BaseIpi,
                AliquotaIpi = i.AliquotaIpi,
                ValorIpi = i.ValorIpi,
                CodigoEnquadramentoIpi = i.CodigoEnquadramentoIpi,
                CstIbsCbs = i.CstIbsCbs,
                CClassTrib = i.CClassTrib,
                BaseIbsCbs = i.BaseIbsCbs,
                AliquotaIbsUf = i.AliquotaIbsUf,
                ValorIbsUf = i.ValorIbsUf,
                AliquotaIbsMunicipio = i.AliquotaIbsMunicipio,
                ValorIbsMunicipio = i.ValorIbsMunicipio,
                AliquotaCbs = i.AliquotaCbs,
                ValorCbs = i.ValorCbs
            }).ToList(),

            Pagamentos = n.Pagamentos.OrderBy(p => p.Ordem).Select(p => new NotaFiscalPagamentoDto
            {
                Id = p.Id,
                FormaPagamento = p.FormaPagamento,
                Valor = p.Valor,
                QuantidadeParcelas = p.QuantidadeParcelas,
                Ordem = p.Ordem
            }).ToList(),

            Eventos = n.Eventos.OrderByDescending(e => e.DataHora).Select(e => new NotaFiscalEventoDto
            {
                Id = e.Id,
                Tipo = e.Tipo,
                DataHora = e.DataHora,
                Sucesso = e.Sucesso,
                NProtEvento = e.NProtEvento,
                CStat = e.CStat,
                XMotivo = e.XMotivo,
                Texto = e.Texto,
                Sequencial = e.Sequencial,
                Usuario = e.Usuario
            }).ToList()
        };
    }
}
