using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Helpers;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class VendaViewModel : BaseViewModel
{
    public const int ItensPorPaginaPadrao = 50;

    private readonly IVendaService _vendaService;
    private readonly INotaFiscalService _notaFiscalService;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _buscaCts;
    private readonly SemaphoreSlim _buscaSemaforo = new(1, 1);

    private ObservableCollection<VendaDto> _vendas = new();
    public ObservableCollection<VendaDto> Vendas { get => _vendas; set => SetProperty(ref _vendas, value); }

    private VendaDto? _vendaSelecionada;
    public VendaDto? VendaSelecionada
    {
        get => _vendaSelecionada;
        set
        {
            SetProperty(ref _vendaSelecionada, value);
            OnPropertyChanged(nameof(TemSelecao));
            OnPropertyChanged(nameof(PodeCancelarVenda));
            OnPropertyChanged(nameof(PodeEmitirNota));
            OnPropertyChanged(nameof(PodeRegistrarTroca));
            OnPropertyChanged(nameof(PodeExcluirVenda));
            NotifyCanExecuteChanged();
        }
    }

    public bool TemSelecao => VendaSelecionada is not null;
    public bool PodeCancelarVenda => TemSelecao && VendaSelecionada?.Status == StatusVenda.Finalizada;
    /// <summary>Venda aberta ainda não tem total definitivo e venda cancelada não tem fato
    /// gerador — nenhuma das duas pode virar nota (o Service recusa; aqui o botão já nasce
    /// desabilitado para o operador não descobrir isso por mensagem de erro).</summary>
    public bool PodeEmitirNota => TemSelecao && VendaSelecionada?.Status == StatusVenda.Finalizada;
    public bool PodeRegistrarTroca => TemSelecao && VendaSelecionada?.Status == StatusVenda.Finalizada;
    public bool PodeExcluirVenda => TemSelecao && VendaSelecionada?.Status != StatusVenda.Aberta;

    private DateTime? _dataInicio = DateTime.Today.AddDays(-30);
    public DateTime? DataInicio
    {
        get => _dataInicio;
        set => SetProperty(ref _dataInicio, value);
    }

    private DateTime? _dataFim = DateTime.Today;
    public DateTime? DataFim
    {
        get => _dataFim;
        set => SetProperty(ref _dataFim, value);
    }

    private string _termoBusca = string.Empty;
    public string TermoBusca
    {
        get => _termoBusca;
        set
        {
            if (!SetPropertyIfChanged(ref _termoBusca, value)) return;
            PaginaAtual = 1;
            _ = BuscarAsync();
        }
    }

    private int _paginaAtual = 1;
    public int PaginaAtual
    {
        get => _paginaAtual;
        set
        {
            if (!SetPropertyIfChanged(ref _paginaAtual, value)) return;
            OnPropertyChanged(nameof(InfoPaginacao));
            OnPropertyChanged(nameof(PodePaginaAnterior));
            OnPropertyChanged(nameof(PodePaginaProxima));
        }
    }

    private int _totalPaginas;
    public int TotalPaginas { get => _totalPaginas; set => SetProperty(ref _totalPaginas, value); }

    private int _totalItens;
    public int TotalItens { get => _totalItens; set => SetProperty(ref _totalItens, value); }

    public string InfoPaginacao => TotalPaginas <= 0 ? "Nenhuma venda" : $"Página {PaginaAtual} de {TotalPaginas}";
    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand NovaVendaCommand { get; }
    public AsyncRelayCommand VisualizarVendaCommand { get; }
    public AsyncRelayCommand CancelarVendaCommand { get; }
    public AsyncRelayCommand ExcluirVendaCommand { get; }
    public AsyncRelayCommand RegistrarTrocaCommand { get; }
    public AsyncRelayCommand ImprimirCupomCommand { get; }
    public AsyncRelayCommand EmitirNotaCommand { get; }
    public AsyncRelayCommand FiltrarCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }

    public VendaViewModel(IVendaService vendaService, INotaFiscalService notaFiscalService, IServiceScopeFactory scopeFactory)
    {
        _vendaService = vendaService;
        _notaFiscalService = notaFiscalService;
        _scopeFactory = scopeFactory;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovaVendaCommand = new AsyncRelayCommand(AbrirPDV);
        VisualizarVendaCommand = new AsyncRelayCommand(VisualizarVenda, () => TemSelecao);
        CancelarVendaCommand = new AsyncRelayCommand(CancelarVenda, () => PodeCancelarVenda && !Carregando);
        ExcluirVendaCommand = new AsyncRelayCommand(ExcluirVenda, () => PodeExcluirVenda && !Carregando);
        RegistrarTrocaCommand = new AsyncRelayCommand(AbrirRegistrarTroca, () => PodeRegistrarTroca && !Carregando);
        ImprimirCupomCommand = new AsyncRelayCommand(ImprimirCupom, () => TemSelecao);
        EmitirNotaCommand = new AsyncRelayCommand(EmitirNota, () => PodeEmitirNota && !Carregando);
        FiltrarCommand = new AsyncRelayCommand(FiltrarAsync);
        PaginaAnteriorCommand = new AsyncRelayCommand(IrPaginaAnterior, () => PodePaginaAnterior);
        PaginaProximaCommand = new AsyncRelayCommand(IrPaginaProxima, () => PodePaginaProxima);
    }

    public async Task CarregarAsync()
    {
        PaginaAtual = Math.Max(1, PaginaAtual);
        await BuscarAsync();
    }

    private async Task FiltrarAsync()
    {
        PaginaAtual = 1;
        await BuscarAsync();
    }

    private async Task BuscarAsync()
    {
        _buscaCts?.Cancel();
        _buscaCts?.Dispose();
        _buscaCts = new CancellationTokenSource();
        var token = _buscaCts.Token;
        var semaforoAdquirido = false;

        try
        {
            await _buscaSemaforo.WaitAsync(token);
            semaforoAdquirido = true;
            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() => Carregando = true);

            var inicio = DataInicio ?? DateTime.Today.AddDays(-30);
            var fim = (DataFim ?? DateTime.Today).Date.AddDays(1).AddSeconds(-1);

            var resultado = await _vendaService.ObterPaginadoPorPeriodoAsync(
                inicio,
                fim,
                PaginaAtual,
                ItensPorPaginaPadrao,
                string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim(),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() =>
            {
                Vendas = new ObservableCollection<VendaDto>(resultado.Itens);
                TotalItens = resultado.TotalItens;
                TotalPaginas = resultado.TotalPaginas;
                if (TotalPaginas > 0 && PaginaAtual > TotalPaginas)
                {
                    PaginaAtual = TotalPaginas;
                    _ = BuscarAsync();
                    return;
                }
                VendaSelecionada = null;
                OnPropertyChanged(nameof(InfoPaginacao));
                OnPropertyChanged(nameof(PodePaginaAnterior));
                OnPropertyChanged(nameof(PodePaginaProxima));
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { UiDispatcher.ExecutarNaUi(() => MostrarErro($"Erro ao carregar vendas: {ex.Message}")); }
        finally
        {
            if (semaforoAdquirido)
            {
                UiDispatcher.ExecutarNaUi(() => Carregando = false);
                _buscaSemaforo.Release();
            }
        }
    }

    private async Task IrPaginaAnterior() { if (PaginaAtual <= 1) return; PaginaAtual--; await BuscarAsync(); }
    private async Task IrPaginaProxima() { if (PaginaAtual >= TotalPaginas) return; PaginaAtual++; await BuscarAsync(); }

    private async Task AbrirPDV()
    {
        try
        {
            using var escopo = _scopeFactory.CreateScope();
            var pdv = escopo.ServiceProvider.GetRequiredService<Views.PDVView>();
            pdv.PrepararFocoBusca();
            if (ModalWindowHelper.ExibirDialogo(pdv) == true)
                await CarregarAsync();
        }
        catch (Exception ex) { MostrarErro($"Erro ao abrir PDV: {ex.Message}"); }
    }

    private async Task VisualizarVenda()
    {
        if (VendaSelecionada is null) return;
        using var escopo = _scopeFactory.CreateScope();
        await WindowHelper.ExibirCupomAsync(escopo.ServiceProvider, VendaSelecionada);
    }

    private async Task AbrirRegistrarTroca()
    {
        if (!ValidarSelecao(
                VendaSelecionada,
                mensagem: "Por favor, selecione uma venda na lista antes de clicar em Registrar Troca."))
            return;

        var vendaSelecionada = VendaSelecionada!;

        if (vendaSelecionada.Status != StatusVenda.Finalizada)
        {
            MostrarErro("Somente vendas finalizadas podem ter itens trocados.");
            return;
        }

        var vendaId = vendaSelecionada.Id;

        try
        {
            var vendaComItens = await _vendaService.ObterComItensAsync(vendaId);
            if (vendaComItens is null)
            {
                MostrarErro("Venda não encontrada ou foi removida.");
                return;
            }

            var itens = vendaComItens.Itens ?? [];
            if (itens.Count == 0)
            {
                MostrarErro("Não foi possível carregar os itens desta venda.");
                return;
            }

            using var escopo = _scopeFactory.CreateScope();
            var modal = escopo.ServiceProvider.GetRequiredService<Views.TrocaFormView>();
            modal.Inicializar(vendaComItens, itens);
            if (ModalWindowHelper.ExibirDialogo(modal) == true)
            {
                MostrarSucesso("Troca registrada com sucesso! Estoque atualizado.");
                await CarregarAsync();
            }
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir registro de troca: {ex.Message}");
        }
    }

    public void ExecutarRegistrarTrocaSeSelecionado()
    {
        if (ValidarSelecao(
                VendaSelecionada,
                mensagem: "Por favor, selecione uma venda na lista antes de clicar em Registrar Troca."))
            RegistrarTrocaCommand.Execute(null);
    }

    private async Task CancelarVenda()
    {
        if (VendaSelecionada is null) return;
        if (VendaSelecionada.Status != StatusVenda.Finalizada)
        {
            MostrarErro("Somente vendas finalizadas podem ser devolvidas.");
            return;
        }

        if (!ConfirmarAcao($"Deseja registrar a devolução da venda #{VendaSelecionada.NumeroVenda}? O estoque será reposto automaticamente."))
            return;

        try
        {
            await _vendaService.CancelarAsync(VendaSelecionada.Id);
            MostrarSucesso("Devolução registrada e estoque reposto com sucesso!");
            await CarregarAsync();
        }
        catch (Exception ex) { MostrarErro($"Erro ao registrar devolução: {ex.Message}"); }
    }

    private async Task ExcluirVenda()
    {
        if (!ValidarSelecao(
                VendaSelecionada,
                entidade: "venda",
                mensagem: "Por favor, selecione uma venda na lista antes de clicar em Excluir Venda."))
            return;

        var venda = VendaSelecionada!;

        var mensagem = "ATENÇÃO: Esta ação é IRREVERSÍVEL.\n\n" +
                       $"A venda #{venda.NumeroVenda} e todos os seus itens serão EXCLUÍDOS PERMANENTEMENTE do banco de dados.";

        if (venda.Status == StatusVenda.Finalizada)
            mensagem += "\n\nO estoque dos produtos vendidos será reposto automaticamente antes da exclusão.";

        mensagem += "\n\nTem certeza absoluta que deseja continuar?";

        if (!ConfirmarAcao(mensagem))
            return;

        try
        {
            await _vendaService.ExcluirFisicamenteAsync(venda.Id);
            MostrarSucesso($"Venda #{venda.NumeroVenda} excluída permanentemente.");
            await CarregarAsync();
        }
        catch (Exception ex) { MostrarErro($"Erro ao excluir venda: {ex.Message}"); }
    }

    private async Task ImprimirCupom()
    {
        if (VendaSelecionada is null) return;
        using var escopo = _scopeFactory.CreateScope();
        await WindowHelper.ExibirCupomAsync(escopo.ServiceProvider, VendaSelecionada);
    }

    /// <summary>
    /// Faturamento da venda: escolher NF-e ou NFC-e, revisar o rascunho montado a partir da
    /// venda (destinatário, itens com a tributação atual do cadastro e os pagamentos) e
    /// seguir direto para "Ações da Nota", onde a emissão de fato acontece.
    ///
    /// O cupom impresso pelo PDV não é documento fiscal — é este fluxo que gera a nota.
    /// </summary>
    private async Task EmitirNota()
    {
        if (!ValidarSelecao(
                VendaSelecionada,
                entidade: "venda",
                mensagem: "Por favor, selecione uma venda na lista antes de clicar em Emitir Nota."))
            return;

        var venda = VendaSelecionada!;

        if (venda.Status != StatusVenda.Finalizada)
        {
            MostrarErro("Somente vendas finalizadas podem ser faturadas em nota fiscal.");
            return;
        }

        try
        {
            var notasDaVenda = await _notaFiscalService.ListarPorVendaAsync(venda.Id);

            if (notasDaVenda.FirstOrDefault(n => NotaFiscalSituacaoHelper.BloqueiaNovaNota(n.Status)) is { } emitida)
            {
                // Nota viva cobrindo a venda: emitir outra seria imposto em dobro sobre a
                // mesma receita. O caminho útil aqui é ir para as ações da nota que existe
                // (DANFE, XML, cancelamento), não criar outra.
                if (ConfirmarAcao(
                        $"A venda #{venda.NumeroVenda} já tem a {DescricaoNota(emitida)}.\n\n" +
                        "Deseja abrir as ações dessa nota (DANFE, XML, cancelamento)?"))
                    AbrirAcoesDaNota(emitida.Id);
                return;
            }

            if (notasDaVenda.FirstOrDefault(n => NotaFiscalSituacaoHelper.PodeRetomar(n.Status)) is { } pendente &&
                ConfirmarAcao(
                    $"A venda #{venda.NumeroVenda} já tem a {DescricaoNota(pendente)}.\n\n" +
                    "Deseja retomar essa nota? (Não = criar outra do zero)"))
            {
                AbrirNotaExistente(pendente);
                return;
            }

            using var escopo = _scopeFactory.CreateScope();

            var dialogoTipo = new Views.SelecionarTipoNotaDialogView(venda, notasDaVenda);
            if (ModalWindowHelper.ExibirDialogo(dialogoTipo) != true || dialogoTipo.TipoSelecionado is not { } tipo)
                return;

            // Monta em memória (nada é gravado ainda) — o operador revisa no formulário e é o
            // "Salvar Rascunho" dele que persiste a nota.
            var rascunho = await _notaFiscalService.MontarRascunhoAPartirDeVendaAsync(venda.Id, tipo);

            var form = new Views.NotaFiscalFormView(escopo.ServiceProvider, rascunho, venda.NumeroVenda);
            if (ModalWindowHelper.ExibirDialogo(form) != true || form.NotaFiscalSalvaId is not { } notaId)
                return;

            AbrirAcoesDaNota(notaId);
        }
        catch (Exception ex)
        {
            MostrarErro(ExceptionMessageHelper.ObterMensagemAmigavel(ex));
        }
    }

    private void AbrirNotaExistente(NotaFiscalResumoDto nota)
    {
        using var escopo = _scopeFactory.CreateScope();
        var form = new Views.NotaFiscalFormView(escopo.ServiceProvider, nota.Tipo, nota.Id)
        {
            ChamadorAbreAcoesAposSalvar = true
        };
        if (ModalWindowHelper.ExibirDialogo(form) == true && form.NotaFiscalSalvaId is { } notaId)
            AbrirAcoesDaNota(notaId);
    }

    /// <summary>"Ações da Nota" é onde a emissão acontece de fato (e depois DANFE, XML,
    /// cancelamento) — a tela de cadastro só grava o rascunho. Encadear as duas evita que o
    /// operador saia de Vendas e vá procurar a nota recém-criada no módulo fiscal.</summary>
    private void AbrirAcoesDaNota(int notaFiscalId)
    {
        using var escopo = _scopeFactory.CreateScope();
        ModalWindowHelper.ExibirDialogo(new Views.NotaFiscalAcoesView(escopo.ServiceProvider, notaFiscalId));
    }

    private static string DescricaoNota(NotaFiscalResumoDto nota)
        => $"{(nota.Tipo == TipoNotaFiscal.NFCe ? "NFC-e" : "NF-e")} {nota.Serie}/{nota.Numero} " +
           $"({NotaFiscalStatusHelper.Descricao(nota.Status)})";

    private bool SetPropertyIfChanged<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
