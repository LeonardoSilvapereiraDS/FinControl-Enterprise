using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

        builder.HasKey(categoria => categoria.Id);

        builder.Property(categoria => categoria.Id)
            .ValueGeneratedOnAdd();

        builder.Property(categoria => categoria.Nome)
            .HasMaxLength(Categoria.TamanhoMaximoNome)
            .IsRequired();

        builder.Property(categoria => categoria.Tipo)
            .IsRequired();

        builder.Property(categoria => categoria.UsuarioId)
            .IsRequired();

        builder.Property(categoria => categoria.Ativa)
            .IsRequired();

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(categoria => categoria.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(categoria => new { categoria.UsuarioId, categoria.Nome, categoria.Tipo })
            .IsUnique();
    }
}
