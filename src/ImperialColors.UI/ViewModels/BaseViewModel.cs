using ImperialColors.UI.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ImperialColors.UI.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _carregando;
    public bool Carregando
    {
        get => _carregando;
        set
        {
            if (EqualityComparer<bool>.Default.Equals(_carregando, value))
                return;

            _carregando = value;
            OnPropertyChanged();
            NotifyCanExecuteChanged();
        }
    }

    private string _mensagemErro = string.Empty;
    public string MensagemErro
    {
        get => _mensagemErro;
        set => SetProperty(ref _mensagemErro, value);
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected void MostrarErro(string mensagem)
    {
        UiDispatcher.ExecutarNaUi(() =>
        {
            MensagemErro = mensagem;
            MessageBox.Show(mensagem, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    protected void MostrarSucesso(string mensagem)
        => UiDispatcher.ExecutarNaUi(() =>
            MessageBox.Show(mensagem, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));

    protected bool ConfirmarAcao(string mensagem)
    {
        var confirmou = false;
        UiDispatcher.ExecutarNaUi(() =>
            confirmou = MessageBox.Show(mensagem, "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
        return confirmou;
    }

    protected void MostrarAvisoSelecao(string entidade = "item", string? mensagem = null)
    {
        UiDispatcher.ExecutarNaUi(() =>
            MessageBox.Show(
                mensagem ?? $"Por favor, selecione um {entidade} na lista para poder editar.",
                "Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning));
    }

    protected bool ValidarSelecao(object? item, string entidade = "item", string? mensagem = null)
    {
        if (item is not null)
            return true;

        MostrarAvisoSelecao(entidade, mensagem);
        return false;
    }

    protected void NotifyCanExecuteChanged()
        => UiDispatcher.ExecutarNaUi(CommandManager.InvalidateRequerySuggested);
}
