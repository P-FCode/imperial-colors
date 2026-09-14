using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImperialColors.UI.Models;

public class ItemOrcamentoFormModel : INotifyPropertyChanged
{
    private int? _produtoId;
    private string _nomeProduto = string.Empty;
    private string? _codigoProduto;
    private string? _unidade;
    private decimal _quantidade = 1;
    private decimal _precoUnitario;

    public int? ProdutoId
    {
        get => _produtoId;
        set { if (_produtoId != value) { _produtoId = value; Notify(); Notify(nameof(ItemManual)); Notify(nameof(TipoDescricao)); } }
    }

    public string NomeProduto
    {
        get => _nomeProduto;
        set { if (_nomeProduto != value) { _nomeProduto = value; Notify(); } }
    }

    public string? CodigoProduto
    {
        get => _codigoProduto;
        set { if (_codigoProduto != value) { _codigoProduto = value; Notify(); } }
    }

    public string? Unidade
    {
        get => _unidade;
        set { if (_unidade != value) { _unidade = value; Notify(); } }
    }

    public decimal Quantidade
    {
        get => _quantidade;
        set { if (_quantidade != value) { _quantidade = value; Notify(); Notify(nameof(Subtotal)); } }
    }

    public decimal PrecoUnitario
    {
        get => _precoUnitario;
        set { if (_precoUnitario != value) { _precoUnitario = value; Notify(); Notify(nameof(Subtotal)); } }
    }

    public decimal Subtotal => Quantidade * PrecoUnitario;
    public bool ItemManual => !ProdutoId.HasValue;
    public string TipoDescricao => ItemManual ? "Manual" : "Estoque";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
