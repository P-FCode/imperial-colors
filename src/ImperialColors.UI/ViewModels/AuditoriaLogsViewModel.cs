using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class AuditoriaLogsViewModel : BaseViewModel
{
    public const int ItensPorPaginaPadrao = 50;

    private readonly IAuditoriaService _auditoriaService;
    private CancellationTokenSource? _buscaCts;
    private readonly SemaphoreSlim _buscaSemaforo = new(1, 1);

    private ObservableCollection<LogAuditoriaDto> _logs = new();
    public ObservableCollection<LogAuditoriaDto> Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    private LogAuditoriaDto? _logSelecionado;
    public LogAuditoriaDto? LogSelecionado
    {
        get => _logSelecionado;
        set
        {
            SetProperty(ref _logSelecionado, value);
            OnPropertyChanged(nameof(TemSelecao));
            NotifyCanExecuteChanged();
        }
    }

    public bool TemSelecao => LogSelecionado is not null;

    private DateTime? _dataInicio = DateTime.Today.AddDays(-7);
    public DateTime? DataInicio
    {
        get => _dataInicio;
        set { if (SetPropertyIfChanged(ref _dataInicio, value)) { PaginaAtual = 1; _ = BuscarAsync(); } }
    }

    private DateTime? _dataFim = DateTime.Today;
    public DateTime? DataFim
    {
        get => _dataFim;
        set { if (SetPropertyIfChanged(ref _dataFim, value)) { PaginaAtual = 1; _ = BuscarAsync(); } }
    }

    private string _moduloSelecionado = "Todos";
    public string ModuloSelecionado
    {
        get => _moduloSelecionado;
        set { if (SetPropertyIfChanged(ref _moduloSelecionado, value)) { PaginaAtual = 1; _ = BuscarAsync(); } }
    }

    private string _nivelSelecionado = "Todos";
    public string NivelSelecionado
    {
        get => _nivelSelecionado;
        set { if (SetPropertyIfChanged(ref _nivelSelecionado, value)) { PaginaAtual = 1; _ = BuscarAsync(); } }
    }

    private string _termoBusca = string.Empty;
    public string TermoBusca
    {
        get => _termoBusca;
        set { if (SetPropertyIfChanged(ref _termoBusca, value)) { PaginaAtual = 1; _ = BuscarAsync(); } }
    }

    public IReadOnlyList<string> Modulos { get; } =
    [
        "Todos", "PDV", "Estoque", "Vendas Externas", "Clientes", "Configurações", "Sistema"
    ];

    public IReadOnlyList<string> Niveis { get; } =
    [
        "Todos", "Informação", "Alerta", "Erro", "Crítico"
    ];

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
    public int TotalPaginas
    {
        get => _totalPaginas;
        set
        {
            SetProperty(ref _totalPaginas, value);
            OnPropertyChanged(nameof(InfoPaginacao));
            OnPropertyChanged(nameof(PodePaginaAnterior));
            OnPropertyChanged(nameof(PodePaginaProxima));
        }
    }

    private int _totalItens;
    public int TotalItens
    {
        get => _totalItens;
        set
        {
            SetProperty(ref _totalItens, value);
            OnPropertyChanged(nameof(InfoPaginacao));
        }
    }

    public string InfoPaginacao => TotalPaginas <= 0
        ? "Nenhum registro"
        : $"Página {PaginaAtual} de {TotalPaginas} — {TotalItens} registro(s)";

    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand FiltrarCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }
    public AsyncRelayCommand VerDetalhesCommand { get; }

    public AuditoriaLogsViewModel(IAuditoriaService auditoriaService)
    {
        _auditoriaService = auditoriaService;
        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        FiltrarCommand = new AsyncRelayCommand(async () => { PaginaAtual = 1; await BuscarAsync(); });
        PaginaAnteriorCommand = new AsyncRelayCommand(IrPaginaAnterior, () => PodePaginaAnterior);
        PaginaProximaCommand = new AsyncRelayCommand(IrPaginaProxima, () => PodePaginaProxima);
        VerDetalhesCommand = new AsyncRelayCommand(AbrirDetalhes, () => TemSelecao);
    }

    public Task CarregarAsync() => BuscarAsync();

    private async Task IrPaginaAnterior()
    {
        if (!PodePaginaAnterior) return;
        PaginaAtual--;
        await BuscarAsync();
    }

    private async Task IrPaginaProxima()
    {
        if (!PodePaginaProxima) return;
        PaginaAtual++;
        await BuscarAsync();
    }

    private Task AbrirDetalhes()
    {
        if (LogSelecionado is null) return Task.CompletedTask;
        UiDispatcher.ExecutarNaUi(() =>
        {
            var janela = new Views.LogAuditoriaDetalheView(LogSelecionado);
            janela.Owner = System.Windows.Application.Current.MainWindow;
            janela.ShowDialog();
        });
        return Task.CompletedTask;
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

            try { await Task.Delay(250, token); }
            catch (OperationCanceledException) { return; }

            UiDispatcher.ExecutarNaUi(() => Carregando = true);

            var filtro = new FiltroLogAuditoriaDto
            {
                DataInicio = DataInicio,
                DataFim = DataFim,
                Modulo = ModuloSelecionado,
                Nivel = ResolverNivel(NivelSelecionado),
                TermoBusca = string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim(),
                Pagina = PaginaAtual,
                ItensPorPagina = ItensPorPaginaPadrao
            };

            var resultado = await _auditoriaService.ObterPaginadoAsync(filtro, token).ConfigureAwait(false);
            if (token.IsCancellationRequested) return;

            UiDispatcher.ExecutarNaUi(() =>
            {
                Logs = new ObservableCollection<LogAuditoriaDto>(resultado.Itens);
                TotalItens = resultado.TotalItens;
                TotalPaginas = resultado.TotalPaginas;
                if (PaginaAtual > TotalPaginas && TotalPaginas > 0)
                    PaginaAtual = TotalPaginas;
                OnPropertyChanged(nameof(InfoPaginacao));
                OnPropertyChanged(nameof(PodePaginaAnterior));
                OnPropertyChanged(nameof(PodePaginaProxima));
                NotifyCanExecuteChanged();
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar auditoria: {ex.Message}");
        }
        finally
        {
            if (semaforoAdquirido) _buscaSemaforo.Release();
            UiDispatcher.ExecutarNaUi(() =>
            {
                Carregando = false;
                NotifyCanExecuteChanged();
            });
        }
    }

    private static NivelLogAuditoria? ResolverNivel(string nivel) => nivel switch
    {
        "Informação" => NivelLogAuditoria.Info,
        "Alerta" => NivelLogAuditoria.Warning,
        "Erro" => NivelLogAuditoria.Error,
        "Crítico" => NivelLogAuditoria.Critical,
        _ => null
    };

    private bool SetPropertyIfChanged<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
