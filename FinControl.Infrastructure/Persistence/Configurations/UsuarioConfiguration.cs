using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(usuario => usuario.Id);

        builder.Property(usuario => usuario.Id)
            .ValueGeneratedOnAdd();

        builder.Property(usuario => usuario.Nome)
            .HasMaxLength(Usuario.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(usuario => usuario.Email)
            .HasMaxLength(Usuario.TamanhoMaximoEmail)
            .IsRequired();

        builder.Property(usuario => usuario.SenhaHash)
            .HasMaxLength(Usuario.TamanhoMaximoSenhaHash)
            .IsRequired();

        builder.Property(usuario => usuario.DataCadastro)
            .IsRequired();

        builder.Property(usuario => usuario.Ativo)
            .IsRequired();

        builder.HasIndex(usuario => usuario.Email)
            .IsUnique();
    }
}
