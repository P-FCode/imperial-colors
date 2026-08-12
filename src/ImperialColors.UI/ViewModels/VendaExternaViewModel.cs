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
    // Antes a tela sempre carregava a tabela inteira (ObterTodosAsync, com Include(Itens))
    // ao abrir — sem limite, diferente de Produtos/Vendas/Clientes/Logs, que já são
    // paginados no banco. Agora carrega em páginas de 100 registros, com "Carregar mais"
    // buscando a próxima página sob demanda.
    private const int ItensPorPagina = 100;

    private readonly IVendaExternaService _vendaExternaService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISessaoService _sessaoService;

    private int _paginaAtual = 1;
    private int _totalRegistros;

    private ObservableCollection<VendaExternaDto> _vendas = new();
    public ObservableCollection<VendaExternaDto> Vendas { get => _vendas; set => SetProperty(ref _vendas, value); }

    public bool TemMaisRegistros => Vendas.Count < _totalRegistros;

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
    public AsyncRelayCommand CarregarMaisCommand { get; }
    public AsyncRelayCommand RegistrarVendaCommand { get; }
    public AsyncRelayCommand EditarVendaCommand { get; }
    public AsyncRelayCommand ExcluirVendaCommand { get; }
    public AsyncRelayCommand RegistrarTrocaCommand { get; }

    public VendaExternaViewModel(
        IVendaExternaService vendaExternaService,
        IServiceScopeFactory scopeFactory,
        ISessaoService sessaoService)
    {
        _vendaExternaService = vendaExternaService;
        _scopeFactory = scopeFactory;
        _sessaoService = sessaoService;

        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        CarregarMaisCommand = new AsyncRelayCommand(CarregarMaisAsync, () => TemMaisRegistros && !Carregando);
        RegistrarVendaCommand = new AsyncRelayCommand(AbrirRegistro);
        EditarVendaCommand = new AsyncRelayCommand(AbrirEdicao, () => TemSelecao && !Carregando);
        ExcluirVendaCommand = new AsyncRelayCommand(ExcluirVenda, () => TemSelecao && !Carregando);
        RegistrarTrocaCommand = new AsyncRelayCommand(AbrirRegistrarTroca, () => TemSelecao && !Carregando);
    }

    public async Task CarregarAsync()
    {
        try
        {
            Carregando = true;
            _paginaAtual = 1;
            var resultado = await _vendaExternaService.ObterPaginadoAsync(_paginaAtual, ItensPorPagina);
            _totalRegistros = resultado.TotalItens;
            Vendas = new ObservableCollection<VendaExternaDto>(resultado.Itens);
            OnPropertyChanged(nameof(TemMaisRegistros));
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

    private async Task CarregarMaisAsync()
    {
        if (!TemMaisRegistros) return;

        try
        {
            Carregando = true;
            var resultado = await _vendaExternaService.ObterPaginadoAsync(_paginaAtual + 1, ItensPorPagina);
            _paginaAtual++;
            _totalRegistros = resultado.TotalItens;
            foreach (var venda in resultado.Itens)
                Vendas.Add(venda);
            OnPropertyChanged(nameof(TemMaisRegistros));
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar mais vendas externas:\n\n{ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
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
