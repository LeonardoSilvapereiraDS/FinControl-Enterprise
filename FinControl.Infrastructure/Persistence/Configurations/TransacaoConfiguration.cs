using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class TransacaoConfiguration : IEntityTypeConfiguration<Transacao>
{
    public void Configure(EntityTypeBuilder<Transacao> builder)
    {
        builder.ToTable("Transacoes");

        builder.HasKey(transacao => transacao.Id);

        builder.Property(transacao => transacao.Id)
            .ValueGeneratedOnAdd();

        builder.Property(transacao => transacao.Descricao)
            .HasMaxLength(Transacao.TamanhoMaximoDescricao)
            .IsRequired();

        builder.Property(transacao => transacao.Valor)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(transacao => transacao.Data)
            .IsRequired();

        builder.Property(transacao => transacao.Tipo)
            .IsRequired();

        builder.Property(transacao => transacao.CategoriaId)
            .IsRequired();

        builder.Property(transacao => transacao.ContaId)
            .IsRequired();

        builder.Property(transacao => transacao.UsuarioId)
            .IsRequired();

        builder.Property(transacao => transacao.Observacao)
            .HasMaxLength(Transacao.TamanhoMaximoObservacao);

        builder.Property(transacao => transacao.Pago)
            .IsRequired();

        builder.Property(transacao => transacao.Recorrente)
            .IsRequired();

        builder.Property(transacao => transacao.DataCadastro)
            .IsRequired();

        builder.Ignore(transacao => transacao.ValorComSinal);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(transacao => transacao.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(transacao => transacao.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Conta>()
            .WithMany()
            .HasForeignKey(transacao => transacao.ContaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transacao => new { transacao.UsuarioId, transacao.Data });
        builder.HasIndex(transacao => transacao.CategoriaId);
        builder.HasIndex(transacao => transacao.ContaId);
    }
}
