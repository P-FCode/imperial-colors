using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class ClienteViewModel : BaseViewModel
{
    public const int ItensPorPaginaPadrao = 50;

    private readonly IClienteService _clienteService;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _buscaCts;
    private readonly SemaphoreSlim _buscaSemaforo = new(1, 1);

    private ObservableCollection<ClienteDto> _clientes = new();
    public ObservableCollection<ClienteDto> Clientes { get => _clientes; set => SetProperty(ref _clientes, value); }

    private ClienteDto? _clienteSelecionado;
    public ClienteDto? ClienteSelecionado
    {
        get => _clienteSelecionado;
        set { SetProperty(ref _clienteSelecionado, value); OnPropertyChanged(nameof(TemSelecao)); NotifyCanExecuteChanged(); }
    }

    public bool TemSelecao => ClienteSelecionado is not null;

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

    public string InfoPaginacao => TotalPaginas <= 0 ? "Nenhum cliente" : $"Página {PaginaAtual} de {TotalPaginas}";
    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand NovoClienteCommand { get; }
    public AsyncRelayCommand EditarClienteCommand { get; }
    public AsyncRelayCommand ExcluirClienteCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }

    public ClienteViewModel(IClienteService clienteService, IServiceScopeFactory scopeFactory)
    {
        _clienteService = clienteService;
        _scopeFactory = scopeFactory;
        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovoClienteCommand = new AsyncRelayCommand(AbrirNovoCliente);
        EditarClienteCommand = new AsyncRelayCommand(AbrirEditarCliente, () => TemSelecao && !Carregando);
        ExcluirClienteCommand = new AsyncRelayCommand(ExcluirCliente, () => TemSelecao && !Carregando);
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

            var resultado = await _clienteService.ObterPaginadoAsync(
                PaginaAtual, ItensPorPaginaPadrao,
                string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim(),
                token).ConfigureAwait(false);

            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() =>
            {
                Clientes = new ObservableCollection<ClienteDto>(resultado.Itens);
                TotalItens = resultado.TotalItens;
                TotalPaginas = resultado.TotalPaginas;
                if (TotalPaginas > 0 && PaginaAtual > TotalPaginas)
                {
                    PaginaAtual = TotalPaginas;
                    _ = BuscarAsync();
                    return;
                }
                ClienteSelecionado = null;
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

    private Task AbrirNovoCliente()
    {
        try
        {
            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.ClienteFormView>();
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

    private async Task AbrirEditarCliente()
    {
        if (!ValidarSelecao(ClienteSelecionado, "cliente"))
            return;

        var clienteId = ClienteSelecionado!.Id;

        try
        {
            var cliente = await _clienteService.ObterPorIdAsync(clienteId);
            if (cliente is null)
            {
                MostrarErro("Cliente não encontrado ou foi removido.");
                return;
            }

            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.ClienteFormView>();
            form.InicializarEdicao(cliente);
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
        if (ValidarSelecao(ClienteSelecionado, "cliente"))
            EditarClienteCommand.Execute(null);
    }

    private async Task ExcluirCliente()
    {
        if (!ValidarSelecao(ClienteSelecionado, "cliente"))
            return;
        if (!ConfirmarAcao($"Deseja excluir permanentemente o cliente '{ClienteSelecionado!.Nome}'?\n\nEsta ação remove o registro do banco e não pode ser desfeita.")) return;
        try
        {
            await _clienteService.RemoverAsync(ClienteSelecionado!.Id);
            MostrarSucesso("Cliente excluído!");
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
