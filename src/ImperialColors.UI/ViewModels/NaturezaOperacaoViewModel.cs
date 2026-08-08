using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using System.Collections.ObjectModel;

namespace ImperialColors.UI.ViewModels;

public class NaturezaOperacaoViewModel : BaseViewModel
{
    private readonly INaturezaOperacaoService _service;

    private ObservableCollection<NaturezaOperacaoDto> _naturezas = new();
    public ObservableCollection<NaturezaOperacaoDto> Naturezas { get => _naturezas; set => SetProperty(ref _naturezas, value); }

    private NaturezaOperacaoDto? _naturezaSelecionada;
    public NaturezaOperacaoDto? NaturezaSelecionada
    {
        get => _naturezaSelecionada;
        set
        {
            SetProperty(ref _naturezaSelecionada, value);
            OnPropertyChanged(nameof(TemSelecao));
            if (value is not null)
                PreencherFormulario(value);
        }
    }

    public bool TemSelecao => NaturezaSelecionada is not null;

    private int _id;
    public int Id { get => _id; set => SetProperty(ref _id, value); }

    private string _descricao = string.Empty;
    public string Descricao { get => _descricao; set => SetProperty(ref _descricao, value); }

    private TipoOperacaoFiscal _tipoOperacao = TipoOperacaoFiscal.Saida;
    public TipoOperacaoFiscal TipoOperacao { get => _tipoOperacao; set => SetProperty(ref _tipoOperacao, value); }

    private FinalidadeNfe _finalidade = FinalidadeNfe.Normal;
    public FinalidadeNfe Finalidade { get => _finalidade; set => SetProperty(ref _finalidade, value); }

    private bool _consumidorFinal = true;
    public bool ConsumidorFinal { get => _consumidorFinal; set => SetProperty(ref _consumidorFinal, value); }

    private string _serie = string.Empty;
    public string Serie { get => _serie; set => SetProperty(ref _serie, value); }

    private string _csosnPadrao = string.Empty;
    public string CsosnPadrao { get => _csosnPadrao; set => SetProperty(ref _csosnPadrao, value); }

    private string _cstIcmsPadrao = string.Empty;
    public string CstIcmsPadrao { get => _cstIcmsPadrao; set => SetProperty(ref _cstIcmsPadrao, value); }

    private string _cfopDentroEstado = string.Empty;
    public string CfopDentroEstado { get => _cfopDentroEstado; set => SetProperty(ref _cfopDentroEstado, value); }

    private string _cfopForaEstado = string.Empty;
    public string CfopForaEstado { get => _cfopForaEstado; set => SetProperty(ref _cfopForaEstado, value); }

    private bool? _difalNaoContribuinte;
    public bool? DifalNaoContribuinte { get => _difalNaoContribuinte; set => SetProperty(ref _difalNaoContribuinte, value); }

    private string _observacoesPadrao = string.Empty;
    public string ObservacoesPadrao { get => _observacoesPadrao; set => SetProperty(ref _observacoesPadrao, value); }

    private bool _modoEdicao;
    public bool ModoEdicao
    {
        get => _modoEdicao;
        set
        {
            SetProperty(ref _modoEdicao, value);
            OnPropertyChanged(nameof(TituloFormulario));
        }
    }

    public string TituloFormulario => ModoEdicao ? "Editar Natureza de Operação" : "Nova Natureza de Operação";

    public AsyncRelayCommand CarregarCommand { get; }
    public AsyncRelayCommand SalvarCommand { get; }
    public RelayCommand NovoCommand { get; }
    public AsyncRelayCommand ExcluirCommand { get; }

    public NaturezaOperacaoViewModel(INaturezaOperacaoService service)
    {
        _service = service;
        CarregarCommand = new AsyncRelayCommand(CarregarAsync);
        SalvarCommand = new AsyncRelayCommand(SalvarAsync);
        NovoCommand = new RelayCommand(Novo);
        ExcluirCommand = new AsyncRelayCommand(ExcluirAsync, () => TemSelecao);
    }

    public async Task CarregarAsync()
    {
        try
        {
            Carregando = true;
            var lista = await _service.ObterTodosAsync();
            Naturezas = new ObservableCollection<NaturezaOperacaoDto>(lista);
        }
        catch (Exception ex)
        {
            MostrarErro($"Erro ao carregar naturezas de operação: {ex.Message}");
        }
        finally
        {
            Carregando = false;
        }
    }

    private void Novo()
    {
        ModoEdicao = false;
        NaturezaSelecionada = null;
        Id = 0;
        Descricao = string.Empty;
        TipoOperacao = TipoOperacaoFiscal.Saida;
        Finalidade = FinalidadeNfe.Normal;
        ConsumidorFinal = true;
        Serie = string.Empty;
        CsosnPadrao = string.Empty;
        CstIcmsPadrao = string.Empty;
        CfopDentroEstado = string.Empty;
        CfopForaEstado = string.Empty;
        DifalNaoContribuinte = null;
        ObservacoesPadrao = string.Empty;
    }

    private void PreencherFormulario(NaturezaOperacaoDto n)
    {
        ModoEdicao = true;
        Id = n.Id;
        Descricao = n.Descricao;
        TipoOperacao = n.TipoOperacao;
        Finalidade = n.Finalidade;
        ConsumidorFinal = n.ConsumidorFinal;
        Serie = n.Serie ?? string.Empty;
        CsosnPadrao = n.CsosnPadrao ?? string.Empty;
        CstIcmsPadrao = n.CstIcmsPadrao ?? string.Empty;
        CfopDentroEstado = n.CfopDentroEstado ?? string.Empty;
        CfopForaEstado = n.CfopForaEstado ?? string.Empty;
        DifalNaoContribuinte = n.DifalNaoContribuinte;
        ObservacoesPadrao = n.ObservacoesPadrao ?? string.Empty;
    }

    private NaturezaOperacaoDto MontarDto() => new()
    {
        Id = Id,
        Descricao = Descricao,
        TipoOperacao = TipoOperacao,
        Finalidade = Finalidade,
        ConsumidorFinal = ConsumidorFinal,
        Serie = string.IsNullOrWhiteSpace(Serie) ? null : Serie,
        CsosnPadrao = string.IsNullOrWhiteSpace(CsosnPadrao) ? null : CsosnPadrao,
        CstIcmsPadrao = string.IsNullOrWhiteSpace(CstIcmsPadrao) ? null : CstIcmsPadrao,
        CfopDentroEstado = string.IsNullOrWhiteSpace(CfopDentroEstado) ? null : CfopDentroEstado,
        CfopForaEstado = string.IsNullOrWhiteSpace(CfopForaEstado) ? null : CfopForaEstado,
        DifalNaoContribuinte = DifalNaoContribuinte,
        ObservacoesPadrao = string.IsNullOrWhiteSpace(ObservacoesPadrao) ? null : ObservacoesPadrao
    };

    private async Task SalvarAsync()
    {
        try
        {
            Carregando = true;
            var dto = MontarDto();

            if (ModoEdicao)
            {
                await _service.AtualizarAsync(dto);
                MostrarSucesso("Natureza de operação atualizada com sucesso!");
            }
            else
            {
                await _service.CriarAsync(dto);
                MostrarSucesso("Natureza de operação criada com sucesso!");
                Novo();
            }

            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
        finally
        {
            Carregando = false;
        }
    }

    private async Task ExcluirAsync()
    {
        if (NaturezaSelecionada is null) return;
        if (!ConfirmarAcao($"Excluir a natureza de operação '{NaturezaSelecionada.Descricao}'?")) return;

        try
        {
            await _service.RemoverAsync(NaturezaSelecionada.Id);
            MostrarSucesso("Natureza de operação excluída.");
            Novo();
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MostrarErro(ex.Message);
        }
    }
}
