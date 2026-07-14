using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class FornecedorFormView : Window
{
    private readonly IFornecedorService _fornecedorService;
    private readonly IViaCepService _viaCepService;
    private readonly ICnpjConsultaService _cnpjConsultaService;
    private int? _fornecedorId;
    private bool _suprimirMascaraCnpj;
    private bool _suprimirMascaraCep;
    private bool _suprimirMascaraTelefone;
    private bool _suprimirMascaraWhatsApp;
    private bool _consultandoCep;
    private bool _consultandoCnpj;
    private CancellationTokenSource? _cepCts;
    private CancellationTokenSource? _cnpjCts;

    public FornecedorFormView(
        IFornecedorService fornecedorService,
        IViaCepService viaCepService,
        ICnpjConsultaService cnpjConsultaService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _fornecedorService = fornecedorService;
        _viaCepService = viaCepService;
        _cnpjConsultaService = cnpjConsultaService;
        EnderecoFormHelper.AplicarMascaraUf(TxtEstado);
    }

    public void InicializarNovo()
    {
        TxtTitulo.Text = "Novo Fornecedor";
        _fornecedorId = null;
        TxtStatus.Text = string.Empty;
    }

    public void InicializarEdicao(FornecedorDto fornecedor)
    {
        ArgumentNullException.ThrowIfNull(fornecedor);

        TxtTitulo.Text = "Editar Fornecedor";
        _fornecedorId = fornecedor.Id;

        _suprimirMascaraCnpj = true;
        TxtCnpj.Text = DocumentoHelper.AplicarMascaraCnpj(fornecedor.Cnpj);
        _suprimirMascaraCnpj = false;
        TxtNome.Text = fornecedor.Nome ?? string.Empty;
        TxtInscricaoEstadual.Text = fornecedor.InscricaoEstadual ?? string.Empty;
        _suprimirMascaraTelefone = true;
        TxtTelefone.Text = DocumentoHelper.AplicarMascaraCelular(fornecedor.Telefone);
        _suprimirMascaraTelefone = false;
        _suprimirMascaraWhatsApp = true;
        TxtWhatsApp.Text = DocumentoHelper.AplicarMascaraCelular(fornecedor.WhatsApp);
        _suprimirMascaraWhatsApp = false;
        TxtEmail.Text = fornecedor.Email ?? string.Empty;
        _suprimirMascaraCep = true;
        TxtCep.Text = DocumentoHelper.AplicarMascaraCep(fornecedor.Cep);
        _suprimirMascaraCep = false;
        TxtLogradouro.Text = fornecedor.Logradouro ?? string.Empty;
        TxtNumero.Text = fornecedor.Numero ?? string.Empty;
        TxtComplemento.Text = fornecedor.Complemento ?? string.Empty;
        TxtBairro.Text = fornecedor.Bairro ?? string.Empty;
        TxtCidade.Text = fornecedor.Cidade ?? string.Empty;
        TxtEstado.Text = fornecedor.Estado ?? string.Empty;
        TxtObservacoes.Text = fornecedor.Observacoes ?? string.Empty;
        TxtStatus.Text = string.Empty;
    }

    private void TxtCnpj_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirMascaraCnpj) return;

        _suprimirMascaraCnpj = true;
        TxtCnpj.Text = DocumentoHelper.AplicarMascaraCnpj(TxtCnpj.Text);
        TxtCnpj.SelectionStart = TxtCnpj.Text.Length;
        _suprimirMascaraCnpj = false;
    }

    private void TxtTelefone_TextChanged(object sender, TextChangedEventArgs e)
        => AplicarMascaraCelular(TxtTelefone, ref _suprimirMascaraTelefone);

    private void TxtWhatsApp_TextChanged(object sender, TextChangedEventArgs e)
        => AplicarMascaraCelular(TxtWhatsApp, ref _suprimirMascaraWhatsApp);

    private static void AplicarMascaraCelular(TextBox textBox, ref bool suprimirFlag)
    {
        if (suprimirFlag) return;

        suprimirFlag = true;
        var cursor = textBox.SelectionStart;
        var textoAntes = textBox.Text[..Math.Min(cursor, textBox.Text.Length)];
        var digitosAntes = DocumentoHelper.SomenteDigitos(textoAntes).Length;
        textBox.Text = DocumentoHelper.AplicarMascaraCelular(textBox.Text);
        textBox.SelectionStart = DocumentoHelper.CalcularPosicaoCursorCelular(textBox.Text, digitosAntes);
        suprimirFlag = false;
    }

    private void TxtCep_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirMascaraCep) return;

        _suprimirMascaraCep = true;
        TxtCep.Text = DocumentoHelper.AplicarMascaraCep(TxtCep.Text);
        TxtCep.SelectionStart = TxtCep.Text.Length;
        _suprimirMascaraCep = false;
    }

    private async void BtnBuscarCep_Click(object sender, RoutedEventArgs e)
        => await ConsultarCepAsync(exigirCepCompleto: true);

    private async void BtnBuscarCnpj_Click(object sender, RoutedEventArgs e)
        => await ConsultarCnpjAsync();

    private async Task ConsultarCepAsync(bool exigirCepCompleto = false)
    {
        if (_consultandoCep)
            return;

        if (!DocumentoHelper.CepCompleto(TxtCep.Text))
        {
            if (exigirCepCompleto)
            {
                TxtStatus.Text = "Informe um CEP válido com 8 dígitos.";
                TxtCep.Focus();
            }
            return;
        }

        _cepCts?.Cancel();
        _cepCts?.Dispose();
        _cepCts = new CancellationTokenSource();
        var token = _cepCts.Token;

        _consultandoCep = true;
        TxtStatus.Text = "Consultando CEP...";

        try
        {
            var endereco = await _viaCepService.ConsultarAsync(TxtCep.Text, token);
            if (token.IsCancellationRequested) return;

            if (endereco is null)
            {
                TxtStatus.Text = "CEP não encontrado.";
                return;
            }

            EnderecoFormHelper.PreencherEndereco(
                TxtLogradouro, TxtBairro, TxtCidade, TxtEstado,
                endereco.Logradouro, endereco.Bairro, endereco.Cidade, endereco.Uf);

            TxtStatus.Text = "Endereço preenchido automaticamente.";
            TxtNumero.Focus();
        }
        catch (OperationCanceledException) { }
        catch
        {
            TxtStatus.Text = "Não foi possível consultar o CEP.";
        }
        finally
        {
            _consultandoCep = false;
        }
    }

    private async Task ConsultarCnpjAsync()
    {
        if (_consultandoCnpj)
            return;

        if (!DocumentoHelper.CnpjCompleto(TxtCnpj.Text))
        {
            TxtStatus.Text = "Informe um CNPJ válido com 14 dígitos.";
            TxtCnpj.Focus();
            return;
        }

        _cnpjCts?.Cancel();
        _cnpjCts?.Dispose();
        _cnpjCts = new CancellationTokenSource();
        var token = _cnpjCts.Token;

        _consultandoCnpj = true;
        TxtStatus.Text = "Consultando CNPJ na Receita...";

        try
        {
            var dados = await _cnpjConsultaService.ConsultarAsync(TxtCnpj.Text, token);
            if (token.IsCancellationRequested) return;

            if (dados is null)
            {
                TxtStatus.Text = "CNPJ não encontrado.";
                return;
            }

            TxtNome.Text = dados.RazaoSocial;

            if (!string.IsNullOrWhiteSpace(dados.Telefone))
            {
                _suprimirMascaraTelefone = true;
                TxtTelefone.Text = DocumentoHelper.AplicarMascaraCelular(dados.Telefone);
                _suprimirMascaraTelefone = false;
            }
            if (!string.IsNullOrWhiteSpace(dados.Email))
                TxtEmail.Text = dados.Email;

            if (!string.IsNullOrWhiteSpace(dados.Cep))
            {
                _suprimirMascaraCep = true;
                TxtCep.Text = dados.Cep;
                _suprimirMascaraCep = false;
            }
            if (!string.IsNullOrWhiteSpace(dados.Logradouro))
                TxtLogradouro.Text = dados.Logradouro;
            if (!string.IsNullOrWhiteSpace(dados.Numero))
                TxtNumero.Text = dados.Numero;
            if (!string.IsNullOrWhiteSpace(dados.Complemento))
                TxtComplemento.Text = dados.Complemento;
            if (!string.IsNullOrWhiteSpace(dados.Bairro))
                TxtBairro.Text = dados.Bairro;
            if (!string.IsNullOrWhiteSpace(dados.Cidade))
                TxtCidade.Text = dados.Cidade;
            if (!string.IsNullOrWhiteSpace(dados.Uf))
                TxtEstado.Text = dados.Uf;

            TxtStatus.Text = "Dados da empresa preenchidos automaticamente.";
            if (string.IsNullOrWhiteSpace(TxtNumero.Text))
                TxtNumero.Focus();
        }
        catch (OperationCanceledException) { }
        catch
        {
            TxtStatus.Text = "Não foi possível consultar o CNPJ.";
        }
        finally
        {
            _consultandoCnpj = false;
        }
    }

    private async void BtnSalvar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtNome.Text))
        {
            MessageBox.Show("Nome é obrigatório.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var dto = new FornecedorDto
            {
                TipoPessoa = TipoPessoa.Juridica,
                Nome = TxtNome.Text.Trim(),
                Cnpj = DocumentoHelper.AplicarMascaraCnpj(TxtCnpj.Text),
                InscricaoEstadual = TxtInscricaoEstadual.Text.Trim(),
                Telefone = TxtTelefone.Text.Trim(),
                WhatsApp = TxtWhatsApp.Text.Trim(),
                Email = TxtEmail.Text.Trim(),
                Cep = TxtCep.Text.Trim(),
                Logradouro = TxtLogradouro.Text.Trim(),
                Numero = TxtNumero.Text.Trim(),
                Complemento = TxtComplemento.Text.Trim(),
                Bairro = TxtBairro.Text.Trim(),
                Cidade = TxtCidade.Text.Trim(),
                Estado = TxtEstado.Text.Trim().ToUpperInvariant(),
                Observacoes = TxtObservacoes.Text.Trim()
            };

            if (_fornecedorId.HasValue)
                await _fornecedorService.AtualizarAsync(_fornecedorId.Value, dto);
            else
                await _fornecedorService.CriarAsync(dto);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);

    private void BtnFechar_Click(object sender, RoutedEventArgs e)
        => ModalWindowHelper.Fechar(this, false);

    protected override void OnClosed(EventArgs e)
    {
        _cepCts?.Cancel();
        _cepCts?.Dispose();
        _cnpjCts?.Cancel();
        _cnpjCts?.Dispose();
        base.OnClosed(e);
    }
}
