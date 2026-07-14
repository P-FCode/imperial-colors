using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class FornecedorViewModel : BaseViewModel
{
    public const int ItensPorPaginaPadrao = 50;

    private readonly IFornecedorService _fornecedorService;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _buscaCts;
    private readonly SemaphoreSlim _buscaSemaforo = new(1, 1);

    private ObservableCollection<FornecedorDto> _fornecedores = new();
    public ObservableCollection<FornecedorDto> Fornecedores { get => _fornecedores; set => SetProperty(ref _fornecedores, value); }

    private FornecedorDto? _fornecedorSelecionado;
    public FornecedorDto? FornecedorSelecionado
    {
        get => _fornecedorSelecionado;
        set { SetProperty(ref _fornecedorSelecionado, value); OnPropertyChanged(nameof(TemSelecao)); NotifyCanExecuteChanged(); }
    }

    public bool TemSelecao => FornecedorSelecionado is not null;

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

    public string InfoPaginacao => TotalPaginas <= 0 ? "Nenhum fornecedor" : $"Página {PaginaAtual} de {TotalPaginas}";
    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand NovoFornecedorCommand { get; }
    public AsyncRelayCommand EditarFornecedorCommand { get; }
    public AsyncRelayCommand ExcluirFornecedorCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }

    public FornecedorViewModel(IFornecedorService fornecedorService, IServiceScopeFactory scopeFactory)
    {
        _fornecedorService = fornecedorService;
        _scopeFactory = scopeFactory;
        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovoFornecedorCommand = new AsyncRelayCommand(AbrirNovo);
        EditarFornecedorCommand = new AsyncRelayCommand(AbrirEditar, () => TemSelecao && !Carregando);
        ExcluirFornecedorCommand = new AsyncRelayCommand(Excluir, () => TemSelecao && !Carregando);
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

            var resultado = await _fornecedorService.ObterPaginadoAsync(
                PaginaAtual, ItensPorPaginaPadrao,
                string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim(),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() =>
            {
                Fornecedores = new ObservableCollection<FornecedorDto>(resultado.Itens);
                TotalItens = resultado.TotalItens;
                TotalPaginas = resultado.TotalPaginas;
                if (TotalPaginas > 0 && PaginaAtual > TotalPaginas)
                {
                    PaginaAtual = TotalPaginas;
                    _ = BuscarAsync();
                    return;
                }
                FornecedorSelecionado = null;
                OnPropertyChanged(nameof(InfoPaginacao));
                OnPropertyChanged(nameof(PodePaginaAnterior));
                OnPropertyChanged(nameof(PodePaginaProxima));
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { UiDispatcher.ExecutarNaUi(() => MostrarErro($"Erro na busca: {ex.Message}")); }
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

    private Task AbrirNovo()
    {
        try
        {
            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.FornecedorFormView>();
            form.InicializarNovo();
            if (ModalWindowHelper.ExibirDialogo(form) == true)
                _ = CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir cadastro: {ex.Message}");
        }
        return Task.CompletedTask;
    }

    private async Task AbrirEditar()
    {
        if (!ValidarSelecao(FornecedorSelecionado, "fornecedor"))
            return;

        var fornecedorId = FornecedorSelecionado!.Id;

        try
        {
            var fornecedor = await _fornecedorService.ObterPorIdAsync(fornecedorId);
            if (fornecedor is null)
            {
                MostrarErro("Fornecedor não encontrado ou foi removido.");
                return;
            }

            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.FornecedorFormView>();
            form.InicializarEdicao(fornecedor);
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
        if (ValidarSelecao(FornecedorSelecionado, "fornecedor"))
            EditarFornecedorCommand.Execute(null);
    }

    private async Task Excluir()
    {
        if (!ValidarSelecao(FornecedorSelecionado, "fornecedor"))
            return;
        if (!ConfirmarAcao($"Deseja excluir permanentemente o fornecedor '{FornecedorSelecionado!.Nome}'?\n\nEsta ação remove o registro do banco e não pode ser desfeita.")) return;
        try
        {
            await _fornecedorService.RemoverAsync(FornecedorSelecionado!.Id);
            MostrarSucesso("Fornecedor excluído!");
            await CarregarAsync();
        }
        catch (Exception ex) { MostrarErro($"Erro: {ex.Message}"); }
    }

    private bool SetPropertyIfChanged<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
