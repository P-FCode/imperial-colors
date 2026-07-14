using ImperialColors.Application.DTOs;
using ImperialColors.Application.Interfaces;
using ImperialColors.Application.Security;
using ImperialColors.Domain.Enums;
using ImperialColors.UI.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace ImperialColors.UI.Views;

public partial class ClienteFormView : Window
{
    private readonly IClienteService _clienteService;
    private readonly IViaCepService _viaCepService;
    private readonly ICnpjConsultaService _cnpjConsultaService;
    private int? _clienteId;
    private bool _suprimirMascaraCpf;
    private bool _suprimirMascaraCnpj;
    private bool _suprimirMascaraCep;
    private bool _suprimirMascaraTelefone;
    private bool _suprimirMascaraWhatsApp;
    private bool _consultandoCep;
    private bool _consultandoCnpj;
    private CancellationTokenSource? _cepCts;
    private CancellationTokenSource? _cnpjCts;

    public ClienteFormView(
        IClienteService clienteService,
        IViaCepService viaCepService,
        ICnpjConsultaService cnpjConsultaService)
    {
        InitializeComponent();
        ModalWindowHelper.AplicarEstiloModerno(this);
        _clienteService = clienteService;
        _viaCepService = viaCepService;
        _cnpjConsultaService = cnpjConsultaService;
        EnderecoFormHelper.AplicarMascaraUf(TxtEstado);

        Loaded += (_, _) =>
        {
            if (RbPf.IsChecked != true && RbPj.IsChecked != true)
                RbPf.IsChecked = true;
            AtualizarPainelTipoPessoa();
        };
    }

    public void InicializarNovo()
    {
        TxtTitulo.Text = "Novo Cliente";
        _clienteId = null;
        TxtStatus.Text = string.Empty;
        RbPf.IsChecked = true;
        AtualizarPainelTipoPessoa();
    }

    public void InicializarEdicao(ClienteDto cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);

        TxtTitulo.Text = "Editar Cliente";
        _clienteId = cliente.Id;

        if (cliente.TipoPessoa == TipoPessoa.Juridica)
            RbPj.IsChecked = true;
        else
            RbPf.IsChecked = true;

        AtualizarPainelTipoPessoa();

        TxtNome.Text = cliente.Nome ?? string.Empty;
        _suprimirMascaraCpf = true;
        TxtCpf.Text = DocumentoHelper.AplicarMascaraCpf(cliente.Cpf);
        _suprimirMascaraCpf = false;
        _suprimirMascaraCnpj = true;
        TxtCnpj.Text = DocumentoHelper.AplicarMascaraCnpj(cliente.Cnpj);
        _suprimirMascaraCnpj = false;
        TxtInscricaoEstadual.Text = cliente.InscricaoEstadual ?? string.Empty;
        _suprimirMascaraTelefone = true;
        TxtTelefone.Text = DocumentoHelper.AplicarMascaraCelular(cliente.Telefone);
        _suprimirMascaraTelefone = false;
        _suprimirMascaraWhatsApp = true;
        TxtWhatsApp.Text = DocumentoHelper.AplicarMascaraCelular(cliente.WhatsApp);
        _suprimirMascaraWhatsApp = false;
        TxtEmail.Text = cliente.Email ?? string.Empty;
        _suprimirMascaraCep = true;
        TxtCep.Text = DocumentoHelper.AplicarMascaraCep(cliente.Cep);
        _suprimirMascaraCep = false;
        TxtLogradouro.Text = cliente.Logradouro ?? string.Empty;
        TxtNumero.Text = cliente.Numero ?? string.Empty;
        TxtComplemento.Text = cliente.Complemento ?? string.Empty;
        TxtBairro.Text = cliente.Bairro ?? string.Empty;
        TxtCidade.Text = cliente.Cidade ?? string.Empty;
        TxtEstado.Text = cliente.Estado ?? string.Empty;
        TxtObservacoes.Text = cliente.Observacoes ?? string.Empty;
        TxtStatus.Text = string.Empty;
    }

    private void TipoPessoaCliente_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || PanelPf is null || PanelPj is null)
            return;

        AtualizarPainelTipoPessoa();
    }

    private void AtualizarPainelTipoPessoa()
    {
        if (PanelPf is null || PanelPj is null || RbPj is null)
            return;

        var isPj = RbPj.IsChecked == true;
        PanelPf.Visibility = isPj ? Visibility.Collapsed : Visibility.Visible;
        PanelPj.Visibility = isPj ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TxtCpf_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suprimirMascaraCpf) return;

        _suprimirMascaraCpf = true;
        var cursor = TxtCpf.SelectionStart;
        var digitsAntes = DocumentoHelper.SomenteDigitos(TxtCpf.Text[..Math.Min(cursor, TxtCpf.Text.Length)]).Length;
        TxtCpf.Text = DocumentoHelper.AplicarMascaraCpf(TxtCpf.Text);
        TxtCpf.SelectionStart = Math.Min(TxtCpf.Text.Length, digitsAntes + (digitsAntes > 3 ? 1 : 0) + (digitsAntes > 6 ? 1 : 0) + (digitsAntes > 9 ? 1 : 0));
        _suprimirMascaraCpf = false;
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

    private void TxtEmail_TextChanged(object sender, TextChangedEventArgs e)
    {
        var email = TxtEmail.Text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            if (TxtStatus.Text.StartsWith("E-mail", StringComparison.OrdinalIgnoreCase))
                TxtStatus.Text = string.Empty;
            return;
        }

        TxtStatus.Text = InputSanitizer.EmailValido(email)
            ? string.Empty
            : "E-mail inválido. Informe um endereço válido (ex.: cliente@email.com) ou deixe em branco.";
    }

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

        var email = TxtEmail.Text.Trim();
        if (!string.IsNullOrEmpty(email) && !InputSanitizer.EmailValido(email))
        {
            MessageBox.Show(
                "Informe um e-mail válido ou deixe o campo em branco.\nExemplo: cliente@email.com",
                "Validação",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            TxtEmail.Focus();
            return;
        }

        try
        {
            var isPj = RbPj.IsChecked == true;
            var dto = new ClienteDto
            {
                TipoPessoa = isPj ? TipoPessoa.Juridica : TipoPessoa.Fisica,
                Nome = TxtNome.Text.Trim(),
                Cpf = isPj ? null : DocumentoHelper.AplicarMascaraCpf(TxtCpf.Text),
                Cnpj = isPj ? DocumentoHelper.AplicarMascaraCnpj(TxtCnpj.Text) : null,
                InscricaoEstadual = isPj ? TxtInscricaoEstadual.Text.Trim() : null,
                Telefone = TxtTelefone.Text.Trim(),
                WhatsApp = TxtWhatsApp.Text.Trim(),
                Email = email,
                Cep = TxtCep.Text.Trim(),
                Logradouro = TxtLogradouro.Text.Trim(),
                Numero = TxtNumero.Text.Trim(),
                Complemento = TxtComplemento.Text.Trim(),
                Bairro = TxtBairro.Text.Trim(),
                Cidade = TxtCidade.Text.Trim(),
                Estado = TxtEstado.Text.Trim().ToUpperInvariant(),
                Observacoes = TxtObservacoes.Text.Trim()
            };

            if (_clienteId.HasValue)
                await _clienteService.AtualizarAsync(_clienteId.Value, dto);
            else
                await _clienteService.CriarAsync(dto);

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
