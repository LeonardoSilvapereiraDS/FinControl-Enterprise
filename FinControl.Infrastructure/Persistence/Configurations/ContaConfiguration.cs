using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class ContaConfiguration : IEntityTypeConfiguration<Conta>
{
    public void Configure(EntityTypeBuilder<Conta> builder)
    {
        builder.ToTable("Contas");

        builder.HasKey(conta => conta.Id);

        builder.Property(conta => conta.Id)
            .ValueGeneratedOnAdd();

        builder.Property(conta => conta.Nome)
            .HasMaxLength(Conta.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(conta => conta.TipoConta)
            .IsRequired();

        builder.Property(conta => conta.SaldoInicial)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(conta => conta.UsuarioId)
            .IsRequired();

        builder.Property(conta => conta.Ativa)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(conta => conta.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(conta => new { conta.UsuarioId, conta.Nome })
            .IsUnique();
    }
}
