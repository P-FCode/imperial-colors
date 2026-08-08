using ImperialColors.Application.DTOs;
using ImperialColors.Domain.Enums;
using ImperialColors.Domain.Exceptions;
using ImperialColors.Domain.Helpers;

namespace ImperialColors.Application.Validation;

/// <summary>
/// Valida o cadastro de cliente: CPF/CNPJ continuam opcionais (nem toda venda precisa
/// de documento), mas quando preenchidos precisam ter dígito verificador válido — um
/// documento com número errado passa despercebido no cadastro e só é descoberto quando
/// a SEFAZ rejeita a nota fiscal na emissão.
/// </summary>
public static class ClienteValidator
{
    public static void Validar(ClienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new DomainException("Nome do cliente é obrigatório.");

        if (dto.TipoPessoa == TipoPessoa.Fisica)
        {
            if (!string.IsNullOrWhiteSpace(dto.Cpf) && !DocumentoFiscalHelper.CpfValido(dto.Cpf))
                throw new DomainException("CPF inválido — confira os dígitos digitados.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(dto.Cnpj) && !DocumentoFiscalHelper.CnpjValido(dto.Cnpj))
                throw new DomainException("CNPJ inválido — confira os dígitos digitados.");
        }
    }
}
