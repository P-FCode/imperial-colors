using ImperialColors.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialColors.Infrastructure.Data.Mappings;

public class ClienteMapping : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(c => c.TipoPessoa).HasColumnName("tipo_pessoa").HasDefaultValue(Domain.Enums.TipoPessoa.Fisica);
        builder.Property(c => c.Nome).HasColumnName("nome").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Cpf).HasColumnName("cpf").HasMaxLength(14);
        builder.Property(c => c.Cnpj).HasColumnName("cnpj").HasMaxLength(18);
        builder.Property(c => c.InscricaoEstadual).HasColumnName("inscricao_estadual").HasMaxLength(20);
        builder.Property(c => c.Telefone).HasColumnName("telefone").HasMaxLength(20);
        builder.Property(c => c.WhatsApp).HasColumnName("whatsapp").HasMaxLength(20);
        builder.Property(c => c.Email).HasColumnName("email").HasMaxLength(200);
        builder.Property(c => c.Cep).HasColumnName("cep").HasMaxLength(10);
        builder.Property(c => c.Logradouro).HasColumnName("logradouro").HasMaxLength(200);
        builder.Property(c => c.Numero).HasColumnName("numero").HasMaxLength(10);
        builder.Property(c => c.Complemento).HasColumnName("complemento").HasMaxLength(100);
        builder.Property(c => c.Bairro).HasColumnName("bairro").HasMaxLength(100);
        builder.Property(c => c.Cidade).HasColumnName("cidade").HasMaxLength(100);
        builder.Property(c => c.Estado).HasColumnName("estado").HasMaxLength(2);
        builder.Property(c => c.Observacoes).HasColumnName("observacoes");
        builder.Property(c => c.CriadoEm).HasColumnName("criado_em");
        builder.Property(c => c.AtualizadoEm).HasColumnName("atualizado_em");
        builder.Property(c => c.Ativo).HasColumnName("ativo");
    }
}
