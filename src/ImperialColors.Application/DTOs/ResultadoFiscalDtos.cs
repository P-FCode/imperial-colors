namespace ImperialColors.Application.DTOs;

/// <summary>
/// Resultado normalizado de uma emissão de NF-e/NFC-e — espelha os campos que o ERP
/// deve gravar (seção 3.1 do GUIA_INTEGRACAO.md). O contrato JSON bruto da API fica
/// inteiramente dentro da Infrastructure; a Application só vê este DTO.
/// </summary>
public class ResultadoEmissaoFiscalDto
{
    public bool Aprovado { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? NProt { get; set; }
    public DateTime? DhRecbto { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? MensagemTraduzida { get; set; }
    public string? QrCodeUrl { get; set; }
    public string? XmlAutorizado { get; set; }
    public string? Erro { get; set; }
    public List<string> Problemas { get; set; } = new();
}

/// <summary>Resultado de cancelamento/carta de correção — seção 8.1/8.2 do guia.</summary>
public class ResultadoEventoFiscalDto
{
    public bool Aprovado { get; set; }
    public string? ChaveAcesso { get; set; }

    /// <summary>Protocolo do EVENTO — distinto do protocolo de autorização da nota.</summary>
    public string? NProt { get; set; }
    public DateTime? DhRegEvento { get; set; }
    public int? NSeqEvento { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? MensagemTraduzida { get; set; }
    public string? Erro { get; set; }
    public List<string> Problemas { get; set; } = new();
}

/// <summary>Resultado de inutilização de faixa — seção 8.3 do guia.</summary>
public class ResultadoInutilizacaoFiscalDto
{
    public bool Aprovado { get; set; }
    public string? NProt { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? Faixa { get; set; }
    public string? Erro { get; set; }
    public List<string> Problemas { get; set; } = new();
}

/// <summary>Status normalizado de uma nota — seção 7.1 do guia.</summary>
public class ResultadoStatusFiscalDto
{
    public string? ChaveAcesso { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? NProt { get; set; }
    public DateTime? DataAutorizacao { get; set; }

    /// <summary>Autorizada | Cancelada | Denegada | Rejeitada | Inexistente | Indeterminada.</summary>
    public string? Situacao { get; set; }
    public bool ConfirmadoNaSefaz { get; set; }
    public List<EventoRegistradoFiscalDto> EventosRegistrados { get; set; } = new();
}

public class EventoRegistradoFiscalDto
{
    public string? TpEvento { get; set; }
    public string? CStat { get; set; }
    public string? XMotivo { get; set; }
    public string? NProt { get; set; }
    public int? NSeqEvento { get; set; }
    public DateTime? DhRegEvento { get; set; }
}

/// <summary>
/// Dados do emitente para o payload <c>emit</c> — vêm de <c>EmpresaConfig</c> (.env) +
/// <c>ConfiguracaoFiscalEmpresa</c> (endereço/CRT), montados pelo NotaFiscalService antes
/// de chamar <see cref="Interfaces.IFiscalApiClient"/>. Fica fora de <c>NotaFiscal</c> por
/// ser dado global da empresa (muda raramente, uma linha só), não por nota.
/// </summary>
public class EmitenteFiscalDto
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string InscricaoEstadual { get; set; } = string.Empty;

    /// <summary>"1"=Simples Nacional, "2"=Simples excesso sublimite, "3"=Regime Normal, "4"=MEI.</summary>
    public string Crt { get; set; } = "1";
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string CodigoMunicipioIbge { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
}
