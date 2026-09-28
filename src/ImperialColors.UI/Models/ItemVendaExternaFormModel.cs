using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImperialColors.UI.Models;

public class ItemVendaExternaFormModel : INotifyPropertyChanged
{
    private int _id;
    private int? _produtoId;
    private string? _codigoBarras;
    private string _nomeProduto = string.Empty;
    private decimal _quantidade = 1;
    private decimal _precoBase;
    private decimal _precoUnitario;
    private decimal _comissao;

    public int Id
    {
        get => _id;
        set { if (_id != value) { _id = value; Notify(); } }
    }

    public int? ProdutoId
    {
        get => _produtoId;
        set { if (_produtoId != value) { _produtoId = value; Notify(); Notify(nameof(TipoDescricao)); Notify(nameof(ItemManual)); } }
    }

    public string? CodigoBarras
    {
        get => _codigoBarras;
        set { if (_codigoBarras != value) { _codigoBarras = value; Notify(); } }
    }

    public string NomeProduto
    {
        get => _nomeProduto;
        set { if (_nomeProduto != value) { _nomeProduto = value; Notify(); } }
    }

    public decimal Quantidade
    {
        get => _quantidade;
        set { if (_quantidade != value) { _quantidade = value; Notify(); Notify(nameof(Subtotal)); } }
    }

    public decimal PrecoBase
    {
        get => _precoBase;
        set { if (_precoBase != value) { _precoBase = value; Notify(); } }
    }

    public decimal PrecoUnitario
    {
        get => _precoUnitario;
        set { if (_precoUnitario != value) { _precoUnitario = value; Notify(); Notify(nameof(Subtotal)); } }
    }

    /// <summary>Comissão deste item — editável direto na grade de conferência, como o
    /// valor praticado e a quantidade.</summary>
    public decimal Comissao
    {
        get => _comissao;
        set { if (_comissao != value) { _comissao = value; Notify(); } }
    }

    public decimal Subtotal => Quantidade * PrecoUnitario;
    public bool ItemManual => !ProdutoId.HasValue;
    public string TipoDescricao => ItemManual ? "Manual" : "Estoque";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
