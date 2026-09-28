using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Exceptions;
using ImperialColors.UI.Helpers;
using ImperialColors.UI.Services;
using ImperialColors.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class VendaExternaViewModel : BaseViewModel
{
    // Paginação de página cheia, como no Histórico de Vendas, Estoque e Clientes: 50 por
    // página, com "Página X de Y" e os botões de anterior/próxima. Antes era uma lista que
    // só crescia ("Carregar mais"), o que deixava a tela com milhares de linhas acumuladas
    // depois de algumas horas de uso e sem como voltar para um trecho já passado.
    public const int ItensPorPaginaPadrao = 50;

    private readonly IVendaExternaService _vendaExternaService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessaoService _sessaoService;

    private ObservableCollection<VendaExternaDto> _vendas = new();
    public ObservableCollection<VendaExternaDto> Vendas { get => _vendas; set => SetProperty(ref _vendas, value); }

    private int _paginaAtual = 1;
    public int PaginaAtual
    {
        get => _paginaAtual;
        set
        {
            SetProperty(ref _paginaAtual, value);
            OnPropertyChanged(nameof(InfoPaginacao));
            OnPropertyChanged(nameof(PodePaginaAnterior));
            OnPropertyChanged(nameof(PodePaginaProxima));
        }
    }

    private int _totalPaginas;
    public int TotalPaginas { get => _totalPaginas; set => SetProperty(ref _totalPaginas, value); }

    private int _totalItens;
    public int TotalItens { get => _totalItens; set => SetProperty(ref _totalItens, value); }

    public string InfoPaginacao => TotalPaginas <= 0
        ? "Nenhuma venda externa"
        : $"Página {PaginaAtual} de {TotalPaginas} — {TotalItens} venda(s)";

    public bool PodePaginaAnterior => PaginaAtual > 1 && !Carregando;
    public bool PodePaginaProxima => PaginaAtual < TotalPaginas && !Carregando;

    private VendaExternaDto? _vendaSelecionada;
    public VendaExternaDto? VendaSelecionada
    {
        get => _vendaSelecionada;
        set
        {
            SetProperty(ref _vendaSelecionada, value);
            OnPropertyChanged(nameof(TemSelecao));
            NotifyCanExecuteChanged();
        }
    }

    public bool TemSelecao => VendaSelecionada is not null;

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand PaginaAnteriorCommand { get; }
    public AsyncRelayCommand PaginaProximaCommand { get; }
    public AsyncRelayCommand RegistrarVendaCommand { get; }
    public AsyncRelayCommand EditarVendaCommand { get; }
    public AsyncRelayCommand ExcluirVendaCommand { get; }
    public AsyncRelayCommand RegistrarTrocaCommand { get; }
    public AsyncRelayCommand AbrirComissoesCommand { get; }

    public VendaExternaViewModel(
        IVendaExternaService vendaExternaService,
        IServiceScopeFactory scopeFactory,
        ISessaoService sessaoService)
    {
        _vendaExternaService = vendaExternaService;
        _scopeFactory = scopeFactory;
        _sessaoService = sessaoService;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        PaginaAnteriorCommand = new AsyncRelayCommand(IrPaginaAnterior, () => PodePaginaAnterior);
        PaginaProximaCommand = new AsyncRelayCommand(IrPaginaProxima, () => PodePaginaProxima);
        RegistrarVendaCommand = new AsyncRelayCommand(AbrirRegistro);
        EditarVendaCommand = new AsyncRelayCommand(AbrirEdicao, () => TemSelecao && !Carregando);
        ExcluirVendaCommand = new AsyncRelayCommand(ExcluirVenda, () => TemSelecao && !Carregando);
        RegistrarTrocaCommand = new AsyncRelayCommand(AbrirRegistrarTroca, () => TemSelecao && !Carregando);
        // Sem exigir seleção: o controle de comissões é sobre o conjunto das vendas, não
        // sobre a linha que está marcada na grade.
        AbrirComissoesCommand = new AsyncRelayCommand(AbrirComissoes, () => !Carregando);
    }

    /// <summary>Recarrega desde a primeira página — usado ao abrir a tela e depois de
    /// registrar, editar ou excluir uma venda.</summary>
    public Task CarregarAsync()
    {
        PaginaAtual = 1;
        return BuscarAsync();
    }

    private async Task BuscarAsync()
    {
        try
        {
            Carregando = true;
            var resultado = await _vendaExternaService.ObterPaginadoAsync(PaginaAtual, ItensPorPaginaPadrao);

            Vendas = new ObservableCollection<VendaExternaDto>(resultado.Itens);
            TotalItens = resultado.TotalItens;
            TotalPaginas = resultado.TotalPaginas;

            // Excluir a última venda de uma página deixa o operador numa página que não
            // existe mais; volta para a última válida em vez de mostrar uma grade vazia.
            if (TotalPaginas > 0 && PaginaAtual > TotalPaginas)
            {
                PaginaAtual = TotalPaginas;
                await BuscarAsync();
                return;
            }

            VendaSelecionada = null;
            OnPropertyChanged(nameof(InfoPaginacao));
            OnPropertyChanged(nameof(PodePaginaAnterior));
            OnPropertyChanged(nameof(PodePaginaProxima));
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar vendas externas:\n\n{ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task IrPaginaAnterior()
    {
        if (PaginaAtual <= 1) return;
        PaginaAtual--;
        await BuscarAsync();
    }

    private async Task IrPaginaProxima()
    {
        if (PaginaAtual >= TotalPaginas) return;
        PaginaAtual++;
        await BuscarAsync();
    }

    /// <summary>
    /// Recarrega a listagem ao fechar: marcar uma comissão como paga muda a coluna Situação
    /// da venda na grade de trás, e sair da janela vendo o valor antigo faria parecer que o
    /// acerto não foi gravado.
    /// </summary>
    private async Task AbrirComissoes()
    {
        using var scope = _scopeFactory.CreateScope();
        var janela = scope.ServiceProvider.GetRequiredService<ComissoesVendaExternaView>();
        janela.Owner = System.Windows.Application.Current.MainWindow;
        janela.ShowDialog();

        await CarregarAsync();
    }

    private async Task AbrirRegistro()
    {
        using var scope = _scopeFactory.CreateScope();
        var form = scope.ServiceProvider.GetRequiredService<VendaExternaFormView>();
        form.Owner = System.Windows.Application.Current.MainWindow;
        form.InicializarNova(_sessaoService.UsuarioAtual?.NomeCompleto);

        if (form.ShowDialog() == true)
            await CarregarAsync();
    }

    private async Task AbrirEdicao()
    {
        if (!ValidarSelecao(VendaSelecionada, "venda externa"))
            return;

        try
        {
            var venda = await _vendaExternaService.ObterPorIdAsync(VendaSelecionada!.Id);
            if (venda is null)
            {
                MostrarErro("Venda externa não encontrada ou foi removida.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var form = scope.ServiceProvider.GetRequiredService<VendaExternaFormView>();
            form.Owner = System.Windows.Application.Current.MainWindow;
            form.InicializarEdicao(venda, _sessaoService.UsuarioAtual?.NomeCompleto);

            if (form.ShowDialog() == true)
                await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao abrir edição: {ex.Message}");
        }
    }

    private async Task ExcluirVenda()
    {
        if (!ValidarSelecao(VendaSelecionada, "venda externa"))
            return;

        var venda = VendaSelecionada!;
        if (!ConfirmarAcao(
                $"Deseja excluir permanentemente a venda externa '{venda.NumeroVendaExterna}'?\n\n" +
                "O estoque dos produtos vinculados será reposto automaticamente.\nEsta ação não pode ser desfeita."))
            return;

        try
        {
            await _vendaExternaService.ExcluirFisicamenteAsync(venda.Id);
            MostrarSucesso("Venda externa excluída e estoque reposto!");
            await CarregarAsync();
        }
        catch (DomainException ex)
        {
            MostrarErro(ex.Message);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao excluir venda externa: {ex.Message}");
        }
    }

    private async Task AbrirRegistrarTroca()
    {
        if (!ValidarSelecao(VendaSelecionada, "venda externa"))
            return;

        try
        {
            var venda = await _vendaExternaService.ObterPorIdAsync(VendaSelecionada!.Id);
            if (venda is null)
            {
                MostrarErro("Venda externa não encontrada ou foi removida.");
                return;
            }

            var itensEstoque = venda.Itens.Where(i => i.ProdutoId.HasValue).ToList();
            if (itensEstoque.Count == 0)
            {
                MostrarErro("Esta venda externa não possui itens vinculados ao estoque para troca.");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var modal = scope.ServiceProvider.GetRequiredService<TrocaFormView>();
            modal.Owner = System.Windows.Application.Current.MainWindow;
            modal.InicializarVendaExterna(venda, itensEstoque, _sessaoService.UsuarioAtual?.NomeCompleto);

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
}
