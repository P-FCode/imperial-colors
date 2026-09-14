using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace ImperialColors.UI.ViewModels;

public class OrcamentoViewModel : BaseViewModel
{
    public const int ItensPorPaginaPadrao = 50;

    private readonly IOrcamentoService _orcamentoService;
    private readonly IRelatorioService _relatorioService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessaoService _sessaoService;
    private CancellationTokenSource? _buscaCts;
    private readonly SemaphoreSlim _buscaSemaforo = new(1, 1);

    private ObservableCollection<OrcamentoDto> _orcamentos = new();
    public ObservableCollection<OrcamentoDto> Orcamentos { get => _orcamentos; set => SetProperty(ref _orcamentos, value); }

    private OrcamentoDto? _orcamentoSelecionado;
    public OrcamentoDto? OrcamentoSelecionado
    {
        get => _orcamentoSelecionado;
        set { SetProperty(ref _orcamentoSelecionado, value); OnPropertyChanged(nameof(TemSelecao)); NotifyCanExecuteChanged(); }
    }

    public bool TemSelecao => OrcamentoSelecionado is not null;

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

    public string InfoPaginacao => TotalPaginas <= 0 ? "Nenhum orçamento" : $"Página {PaginaAtual} de {TotalPaginas} ({TotalItens} orçamento(s))";
    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand NovoOrcamentoCommand { get; }
    public AsyncRelayCommand EditarOrcamentoCommand { get; }
    public AsyncRelayCommand ExcluirOrcamentoCommand { get; }
    public AsyncRelayCommand GerarPdfCommand { get; }
    public AsyncRelayCommand AprovarCommand { get; }
    public AsyncRelayCommand RecusarCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }

    public OrcamentoViewModel(
        IOrcamentoService orcamentoService,
        IRelatorioService relatorioService,
        IServiceScopeFactory scopeFactory,
        ISessaoService sessaoService)
    {
        _orcamentoService = orcamentoService;
        _relatorioService = relatorioService;
        _scopeFactory = scopeFactory;
        _sessaoService = sessaoService;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovoOrcamentoCommand = new AsyncRelayCommand(AbrirNovo);
        EditarOrcamentoCommand = new AsyncRelayCommand(AbrirEdicao, () => TemSelecao && !Carregando);
        ExcluirOrcamentoCommand = new AsyncRelayCommand(Excluir, () => TemSelecao && !Carregando);
        GerarPdfCommand = new AsyncRelayCommand(GerarPdf, () => TemSelecao && !Carregando);
        AprovarCommand = new AsyncRelayCommand(() => AlterarStatus(StatusOrcamento.Aprovado), () => TemSelecao && !Carregando);
        RecusarCommand = new AsyncRelayCommand(() => AlterarStatus(StatusOrcamento.Recusado), () => TemSelecao && !Carregando);
        PaginaAnteriorCommand = new AsyncRelayCommand(IrPaginaAnterior, () => PodePaginaAnterior);
        PaginaProximaCommand = new AsyncRelayCommand(IrPaginaProxima, () => PodePaginaProxima);
    }

    public async Task CarregarAsync()
    {
        PaginaAtual = Math.Max(1, PaginaAtual);
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

            try { await Task.Delay(300, token); }
            catch (OperationCanceledException) { return; }

            UiDispatcher.ExecutarNaUi(() => Carregando = true);

            var resultado = await _orcamentoService.ObterPaginadoAsync(
                PaginaAtual, ItensPorPaginaPadrao,
                string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim(),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() =>
            {
                Orcamentos = new ObservableCollection<OrcamentoDto>(resultado.Itens);
                TotalItens = resultado.TotalItens;
                TotalPaginas = resultado.TotalPaginas;
                if (TotalPaginas > 0 && PaginaAtual > TotalPaginas)
                {
                    PaginaAtual = TotalPaginas;
                    _ = BuscarAsync();
                    return;
                }
                OrcamentoSelecionado = null;
                OnPropertyChanged(nameof(InfoPaginacao));
                OnPropertyChanged(nameof(PodePaginaAnterior));
                OnPropertyChanged(nameof(PodePaginaProxima));
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { UiDispatcher.ExecutarNaUi(() => MostrarErro($"Erro ao carregar orçamentos:\n\n{ex.Message}")); }
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

    private async Task AbrirNovo()
    {
        try
        {
            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<OrcamentoFormView>();
            form.InicializarNovo(_sessaoService.UsuarioAtual?.NomeCompleto);

            if (ModalWindowHelper.ExibirDialogo(form) == true)
                await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir orçamento: {ex.Message}");
        }
    }

    private async Task AbrirEdicao()
    {
        if (!ValidarSelecao(OrcamentoSelecionado, "orçamento"))
            return;

        try
        {
            var orcamento = await _orcamentoService.ObterPorIdAsync(OrcamentoSelecionado!.Id);
            if (orcamento is null)
            {
                MostrarErro("Orçamento não encontrado ou foi removido.");
                return;
            }

            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<OrcamentoFormView>();
            form.InicializarEdicao(orcamento, _sessaoService.UsuarioAtual?.NomeCompleto);

            if (ModalWindowHelper.ExibirDialogo(form) == true)
                await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir edição: {ex.Message}");
        }
    }

    public void ExecutarEdicaoSeSelecionado()
    {
        if (TemSelecao && EditarOrcamentoCommand.CanExecute(null))
            EditarOrcamentoCommand.Execute(null);
    }

    private async Task Excluir()
    {
        if (!ValidarSelecao(OrcamentoSelecionado, "orçamento"))
            return;

        var orcamento = OrcamentoSelecionado!;
        if (!ConfirmarAcao(
                $"Deseja excluir permanentemente o orçamento '{orcamento.NumeroOrcamento}'?\n\n" +
                "Orçamento não movimenta estoque nem financeiro, então nada mais será afetado.\nEsta ação não pode ser desfeita."))
            return;

        try
        {
            await _orcamentoService.RemoverAsync(orcamento.Id);
            MostrarSucesso("Orçamento excluído!");
            await CarregarAsync();
        }
        catch (DomainException ex) { MostrarErro(ex.Message); }
        catch (Exception ex) { MostrarErro($"Erro ao excluir orçamento: {ex.Message}"); }
    }

    private async Task AlterarStatus(StatusOrcamento status)
    {
        if (!ValidarSelecao(OrcamentoSelecionado, "orçamento"))
            return;

        var orcamento = OrcamentoSelecionado!;
        var rotulo = status == StatusOrcamento.Aprovado ? "aprovado" : "recusado";

        if (!ConfirmarAcao($"Marcar o orçamento '{orcamento.NumeroOrcamento}' como {rotulo}?"))
            return;

        try
        {
            await _orcamentoService.AlterarStatusAsync(orcamento.Id, status);
            MostrarSucesso($"Orçamento marcado como {rotulo}.");
            await CarregarAsync();
        }
        catch (DomainException ex) { MostrarErro(ex.Message); }
        catch (Exception ex) { MostrarErro($"Erro ao alterar situação: {ex.Message}"); }
    }

    private async Task GerarPdf()
    {
        if (!ValidarSelecao(OrcamentoSelecionado, "orçamento"))
            return;

        try
        {
            var orcamento = await _orcamentoService.ObterPorIdAsync(OrcamentoSelecionado!.Id);
            if (orcamento is null)
            {
                MostrarErro("Orçamento não encontrado ou foi removido.");
                return;
            }

            var caminho = string.Empty;
            var confirmou = false;

            UiDispatcher.ExecutarNaUi(() =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Orcamento_{orcamento.NumeroOrcamento}",
                    DefaultExt = ".pdf",
                    Filter = "PDF|*.pdf"
                };

                confirmou = dialog.ShowDialog() == true;
                caminho = dialog.FileName;
            });

            if (!confirmou)
                return;

            await _relatorioService.GerarOrcamentoPdfAsync(orcamento, caminho);

            MostrarSucesso($"Orçamento gerado em:\n{caminho}");

            // O PDF já foi totalmente gravado e o arquivo liberado neste ponto.
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = caminho,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao gerar PDF do orçamento: {ex.Message}");
        }
    }

    private bool SetPropertyIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
