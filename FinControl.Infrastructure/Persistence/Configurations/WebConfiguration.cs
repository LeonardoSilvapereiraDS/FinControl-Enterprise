using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace FinControl.Infrastructure.Persistence.Configurations;

public sealed class AcessoConfiguration : IEntityTypeConfiguration<AcessoWeb>
{
    public void Configure(EntityTypeBuilder<AcessoWeb> b)
    {
        b.ToTable("AcessosWeb"); b.HasKey(x => x.Id);
        b.HasIndex(x => x.Email).IsUnique();
        b.Property(x => x.Nome).HasMaxLength(120); b.Property(x => x.Email).HasMaxLength(180);
        b.Property(x => x.SenhaHash).HasMaxLength(255);
    }
}
public sealed class EspacoConfiguration : IEntityTypeConfiguration<EspacoFinanceiro>
{
    public void Configure(EntityTypeBuilder<EspacoFinanceiro> b)
    {
        b.ToTable("EspacosFinanceiros"); b.HasKey(x => x.Id); b.HasIndex(x => x.UsuarioId).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class MembroConfiguration : IEntityTypeConfiguration<MembroEspaco>
{
    public void Configure(EntityTypeBuilder<MembroEspaco> b)
    {
        b.ToTable("MembrosEspaco"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.EspacoId, x.AcessoId }).IsUnique();
        b.HasOne<EspacoFinanceiro>().WithMany().HasForeignKey(x => x.EspacoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<AcessoWeb>().WithMany().HasForeignKey(x => x.AcessoId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class RecorrenciaConfiguration : IEntityTypeConfiguration<Recorrencia>
{
    public void Configure(EntityTypeBuilder<Recorrencia> b)
    {
        b.ToTable("Recorrencias"); b.HasKey(x => x.Id); b.Property(x => x.Valor).HasPrecision(18,2);
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Categoria>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Conta>().WithMany().HasForeignKey(x => x.ContaId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class OcorrenciaConfiguration : IEntityTypeConfiguration<OcorrenciaRecorrente>
{
    public void Configure(EntityTypeBuilder<OcorrenciaRecorrente> b)
    {
        b.ToTable("OcorrenciasRecorrentes"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RecorrenciaId, x.Data }).IsUnique();
        b.HasOne<Recorrencia>().WithMany().HasForeignKey(x => x.RecorrenciaId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class RegistroImportadoConfiguration : IEntityTypeConfiguration<RegistroImportado>
{
    public void Configure(EntityTypeBuilder<RegistroImportado> b)
    {
        b.ToTable("RegistrosImportados"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.UsuarioId, x.Chave }).IsUnique();
        b.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
