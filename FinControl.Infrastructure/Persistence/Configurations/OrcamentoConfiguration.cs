using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class OrcamentoConfiguration : IEntityTypeConfiguration<Orcamento>
{
    public void Configure(EntityTypeBuilder<Orcamento> builder)
    {
        builder.ToTable("Orcamentos");

        builder.HasKey(orcamento => orcamento.Id);

        builder.Property(orcamento => orcamento.Id)
            .ValueGeneratedOnAdd();

        builder.Property(orcamento => orcamento.CategoriaId)
            .IsRequired();

        builder.Property(orcamento => orcamento.UsuarioId)
            .IsRequired();

        builder.Property(orcamento => orcamento.ValorLimite)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(orcamento => orcamento.Mes)
            .IsRequired();

        builder.Property(orcamento => orcamento.Ano)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(orcamento => orcamento.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(orcamento => orcamento.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(orcamento => new
            {
                orcamento.UsuarioId,
                orcamento.CategoriaId,
                orcamento.Mes,
                orcamento.Ano
            })
            .IsUnique();
    }
}
