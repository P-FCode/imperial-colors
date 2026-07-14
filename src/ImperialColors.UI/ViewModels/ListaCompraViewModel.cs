using ImperialColors.Application.DTOs;
using ImperialColors.Application.Helpers;
using ImperialColors.Application.Interfaces;
using ImperialColors.UI.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace ImperialColors.UI.ViewModels;

public class ListaCompraViewModel : BaseViewModel
{
    private readonly IListaCompraService _listaCompraService;
    private readonly IFornecedorService _fornecedorService;
    private readonly IServiceScopeFactory _scopeFactory;

    private ObservableCollection<ListaCompraDto> _listas = new();
    public ObservableCollection<ListaCompraDto> Listas { get => _listas; set => SetProperty(ref _listas, value); }

    private ListaCompraDto? _listaSelecionada;
    public ListaCompraDto? ListaSelecionada
    {
        get => _listaSelecionada;
        set
        {
            SetProperty(ref _listaSelecionada, value);
            OnPropertyChanged(nameof(TemSelecao));
            OnPropertyChanged(nameof(PodeFinalizar));
            OnPropertyChanged(nameof(PodeReabrir));
            OnPropertyChanged(nameof(PodeEnviarWhatsApp));
            OnPropertyChanged(nameof(PodeVisualizarNota));
            NotifyCanExecuteChanged();
        }
    }

    public bool TemSelecao => ListaSelecionada is not null;
    public bool PodeFinalizar => TemSelecao && ListaSelecionada is { Finalizada: false };
    public bool PodeReabrir => TemSelecao && ListaSelecionada is { Finalizada: true };
    public bool PodeEnviarWhatsApp => TemSelecao
        && ListaSelecionada!.FornecedorId is > 0
        && ListaSelecionada.TotalItens > 0;
    public bool PodeVisualizarNota => TemSelecao && ListaSelecionada!.PossuiNotaFiscal;

    private string _termoBusca = string.Empty;
    public string TermoBusca
    {
        get => _termoBusca;
        set
        {
            if (!SetPropertyIfChanged(ref _termoBusca, value)) return;
            _ = CarregarAsync();
        }
    }

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand NovaListaCommand { get; }
    public AsyncRelayCommand EditarListaCommand { get; }
    public AsyncRelayCommand ExcluirListaCommand { get; }
    public AsyncRelayCommand FinalizarListaCommand { get; }
    public AsyncRelayCommand ReabrirListaCommand { get; }
    public AsyncRelayCommand EnviarWhatsAppCommand { get; }
    public AsyncRelayCommand AnexarNotaCommand { get; }
    public AsyncRelayCommand VisualizarNotaCommand { get; }

    public ListaCompraViewModel(
        IListaCompraService listaCompraService,
        IFornecedorService fornecedorService,
        IServiceScopeFactory scopeFactory)
    {
        _listaCompraService = listaCompraService;
        _fornecedorService = fornecedorService;
        _scopeFactory = scopeFactory;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        NovaListaCommand = new AsyncRelayCommand(AbrirNova);
        EditarListaCommand = new AsyncRelayCommand(AbrirEditar, () => TemSelecao && !Carregando);
        ExcluirListaCommand = new AsyncRelayCommand(Excluir, () => TemSelecao && !Carregando);
        FinalizarListaCommand = new AsyncRelayCommand(Finalizar, () => PodeFinalizar && !Carregando);
        ReabrirListaCommand = new AsyncRelayCommand(Reabrir, () => PodeReabrir && !Carregando);
        EnviarWhatsAppCommand = new AsyncRelayCommand(EnviarWhatsApp, () => PodeEnviarWhatsApp && !Carregando);
        AnexarNotaCommand = new AsyncRelayCommand(AnexarNota, () => TemSelecao && !Carregando);
        VisualizarNotaCommand = new AsyncRelayCommand(VisualizarNota, () => PodeVisualizarNota && !Carregando);
    }

    public async Task CarregarAsync()
    {
        try
        {
            Carregando = true;
            var listas = await _listaCompraService.ObterTodosAsync(
                string.IsNullOrWhiteSpace(TermoBusca) ? null : TermoBusca.Trim());
            Listas = new ObservableCollection<ListaCompraDto>(listas);
            ListaSelecionada = null;
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar listas: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private Task AbrirNova()
    {
        try
        {
            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.ListaCompraFormView>();
            form.InicializarNova();
            if (ModalWindowHelper.ExibirDialogo(form) == true)
                _ = CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir lista: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private async Task AbrirEditar()
    {
        if (!ValidarSelecao(ListaSelecionada, "lista de compras"))
            return;

        var listaId = ListaSelecionada!.Id;

        try
        {
            var lista = await _listaCompraService.ObterPorIdAsync(listaId);
            if (lista is null)
            {
                MostrarErro("Lista não encontrada ou foi removida.");
                return;
            }

            using var escopo = _scopeFactory.CreateScope();
            var form = escopo.ServiceProvider.GetRequiredService<Views.ListaCompraFormView>();
            form.InicializarEdicao(lista);
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
        if (ValidarSelecao(ListaSelecionada, "lista de compras"))
            EditarListaCommand.Execute(null);
    }

    private async Task Excluir()
    {
        if (!ValidarSelecao(ListaSelecionada, "lista de compras"))
            return;
        if (!ConfirmarAcao($"Deseja excluir a lista '{ListaSelecionada!.Nome}'?")) return;

        try
        {
            await _listaCompraService.RemoverAsync(ListaSelecionada!.Id);
            MostrarSucesso("Lista excluída!");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao excluir: {ex.Message}");
        }
    }

    private async Task Finalizar()
    {
        if (ListaSelecionada is null) return;
        try
        {
            await _listaCompraService.AlterarFinalizadaAsync(ListaSelecionada.Id, true);
            MostrarSucesso("Lista finalizada!");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao finalizar: {ex.Message}");
        }
    }

    private async Task Reabrir()
    {
        if (ListaSelecionada is null) return;
        try
        {
            await _listaCompraService.AlterarFinalizadaAsync(ListaSelecionada.Id, false);
            MostrarSucesso("Lista reaberta!");
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao reabrir: {ex.Message}");
        }
    }

    private async Task EnviarWhatsApp()
    {
        if (!ValidarSelecao(ListaSelecionada, "lista de compras"))
            return;

        var listaResumo = ListaSelecionada!;
        if (listaResumo.FornecedorId is not > 0)
        {
            MostrarErro("Selecione uma lista vinculada a um fornecedor para enviar pelo WhatsApp.");
            return;
        }

        try
        {
            var lista = await _listaCompraService.ObterPorIdAsync(listaResumo.Id);
            if (lista is null)
            {
                MostrarErro("Lista não encontrada ou foi removida.");
                return;
            }

            if (lista.Itens.Count == 0)
            {
                MostrarErro("A lista precisa conter pelo menos um item para envio.");
                return;
            }

            var fornecedor = await _fornecedorService.ObterPorIdAsync(lista.FornecedorId!.Value);
            if (fornecedor is null)
            {
                MostrarErro("Fornecedor não encontrado.");
                return;
            }

            var telefone = ListaCompraWhatsAppHelper.NormalizarTelefoneWhatsApp(fornecedor.WhatsApp, fornecedor.Telefone);
            if (telefone is null)
            {
                MostrarErro($"O fornecedor '{fornecedor.Nome}' não possui WhatsApp ou telefone válido cadastrado.");
                return;
            }

            var mensagem = ListaCompraWhatsAppHelper.MontarMensagemPedido(fornecedor.Nome, lista.Itens);
            WhatsAppHelper.AbrirChat(telefone, mensagem);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao preparar envio pelo WhatsApp: {ex.Message}");
        }
    }

    private async Task AnexarNota()
    {
        if (!ValidarSelecao(ListaSelecionada, "lista de compras"))
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Anexar Nota da Compra",
            Filter = "Documentos e imagens|*.pdf;*.png;*.jpg;*.jpeg|PDF (*.pdf)|*.pdf|Imagens (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            Carregando = true;
            var listaId = ListaSelecionada!.Id;
            var bytes = await File.ReadAllBytesAsync(dialog.FileName);
            await _listaCompraService.AnexarNotaFiscalAsync(listaId, bytes, Path.GetFileName(dialog.FileName));
            MostrarSucesso("Nota fiscal anexada com sucesso!");
            await CarregarAsync();
            ListaSelecionada = Listas.FirstOrDefault(l => l.Id == listaId);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao anexar nota fiscal: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task VisualizarNota()
    {
        if (!ValidarSelecao(ListaSelecionada, "lista de compras"))
            return;

        try
        {
            Carregando = true;
            var nota = await _listaCompraService.ObterNotaFiscalAsync(ListaSelecionada!.Id);
            if (nota is null)
            {
                MostrarErro("Esta lista não possui nota fiscal anexada.");
                return;
            }

            await ArquivoAnexoHelper.AbrirNoVisualizadorPadraoAsync(nota.Conteudo, nota.NomeArquivo);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao visualizar nota fiscal: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private bool SetPropertyIfChanged<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value!;
        OnPropertyChanged(propertyName);
        return true;
    }
}
