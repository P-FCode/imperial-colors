using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class FornecedorMapping : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("fornecedores");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(f => f.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(f => f.TipoPessoa).HasColumnName("tipo_pessoa").HasDefaultValue(Domain.Enums.TipoPessoa.Juridica);
        builder.Property(f => f.Cnpj).HasColumnName("cnpj").HasMaxLength(18);
        builder.Property(f => f.InscricaoEstadual).HasColumnName("inscricao_estadual").HasMaxLength(20);
        builder.Property(f => f.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(f => f.WhatsApp).HasColumnName("whatsapp").HasMaxLength(20);
        builder.Property(f => f.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(f => f.Cep).HasColumnName("cep").HasMaxLength(10);
        builder.Property(f => f.Logradouro).HasColumnName("logradouro").HasMaxLength(200);
        builder.Property(f => f.Numero).HasColumnName("numero").HasMaxLength(10);
        builder.Property(f => f.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(f => f.Bairro).HasColumnName("bairro").HasMaxLength(100);
        builder.Property(f => f.Cidade).HasColumnName("cidade").HasMaxLength(100);
        builder.Property(f => f.Estado).HasColumnName("estado").HasMaxLength(2);
        builder.Property(f => f.Observacoes).HasColumnName("observacoes");
        builder.Property(f => f.CriadoEm).HasColumnName("criado_em");
        builder.Property(f => f.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(f => f.Ativo).HasColumnName("ativo");
    }
}
