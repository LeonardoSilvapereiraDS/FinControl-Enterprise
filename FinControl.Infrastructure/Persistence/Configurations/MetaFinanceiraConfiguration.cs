using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class MetaFinanceiraConfiguration : IEntityTypeConfiguration<MetaFinanceira>
{
    public void Configure(EntityTypeBuilder<MetaFinanceira> builder)
    {
        builder.ToTable("MetasFinanceiras");

        builder.HasKey(meta => meta.Id);

        builder.Property(meta => meta.Id)
            .ValueGeneratedOnAdd();

        builder.Property(meta => meta.Nome)
            .HasMaxLength(MetaFinanceira.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(meta => meta.Descricao)
            .HasMaxLength(MetaFinanceira.TamanhoMaximoDescricao);

        builder.Property(meta => meta.ValorObjetivo)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(meta => meta.ValorAtual)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(meta => meta.DataInicio)
            .IsRequired();

        builder.Property(meta => meta.DataLimite)
            .IsRequired();

        builder.Property(meta => meta.UsuarioId)
            .IsRequired();

        builder.Property(meta => meta.Concluida)
            .IsRequired();

        builder.Ignore(meta => meta.PercentualProgresso);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(meta => meta.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(meta => new { meta.UsuarioId, meta.Nome });
    }
}
