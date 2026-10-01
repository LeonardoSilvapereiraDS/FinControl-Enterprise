using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace FinControl.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FinControlDbContext))]
[Migration("20261001010000_WebMultiEspaco")]
public sealed class WebMultiEspaco : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.Sql("""
            CREATE TABLE AcessosWeb (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL, Email TEXT NOT NULL,
              SenhaHash TEXT NOT NULL, SeloSeguranca TEXT NOT NULL, Tentativas INTEGER NOT NULL, BloqueadoAte TEXT NULL);
            CREATE UNIQUE INDEX IX_AcessosWeb_Email ON AcessosWeb(Email);
            CREATE TABLE EspacosFinanceiros (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL,
              Perfil TEXT NOT NULL, UsuarioId INTEGER NOT NULL REFERENCES Usuarios(Id) ON DELETE RESTRICT);
            CREATE UNIQUE INDEX IX_EspacosFinanceiros_UsuarioId ON EspacosFinanceiros(UsuarioId);
            CREATE TABLE MembrosEspaco (Id INTEGER PRIMARY KEY AUTOINCREMENT,
              EspacoId INTEGER NOT NULL REFERENCES EspacosFinanceiros(Id) ON DELETE CASCADE,
              AcessoId INTEGER NOT NULL REFERENCES AcessosWeb(Id) ON DELETE CASCADE, Papel TEXT NOT NULL);
            CREATE UNIQUE INDEX IX_MembrosEspaco_EspacoId_AcessoId ON MembrosEspaco(EspacoId,AcessoId);
            CREATE INDEX IX_MembrosEspaco_AcessoId ON MembrosEspaco(AcessoId);
            CREATE TABLE Recorrencias (Id INTEGER PRIMARY KEY AUTOINCREMENT, UsuarioId INTEGER NOT NULL REFERENCES Usuarios(Id) ON DELETE RESTRICT,
              Descricao TEXT NOT NULL, Valor TEXT NOT NULL, Tipo INTEGER NOT NULL,
              CategoriaId INTEGER NOT NULL REFERENCES Categorias(Id) ON DELETE RESTRICT,
              ContaId INTEGER NOT NULL REFERENCES Contas(Id) ON DELETE RESTRICT,
              Frequencia TEXT NOT NULL, Inicio TEXT NOT NULL, Fim TEXT NULL, ProximaData TEXT NOT NULL, Ativa INTEGER NOT NULL);
            CREATE INDEX IX_Recorrencias_UsuarioId ON Recorrencias(UsuarioId);
            CREATE INDEX IX_Recorrencias_CategoriaId ON Recorrencias(CategoriaId);
            CREATE INDEX IX_Recorrencias_ContaId ON Recorrencias(ContaId);
            CREATE TABLE OcorrenciasRecorrentes (Id INTEGER PRIMARY KEY AUTOINCREMENT,
              RecorrenciaId INTEGER NOT NULL REFERENCES Recorrencias(Id) ON DELETE CASCADE, Data TEXT NOT NULL, TransacaoId INTEGER NOT NULL);
            CREATE UNIQUE INDEX IX_OcorrenciasRecorrentes_RecorrenciaId_Data ON OcorrenciasRecorrentes(RecorrenciaId,Data);
            CREATE TABLE RegistrosImportados (Id INTEGER PRIMARY KEY AUTOINCREMENT,
              UsuarioId INTEGER NOT NULL REFERENCES Usuarios(Id) ON DELETE RESTRICT, Chave TEXT NOT NULL, TransacaoId INTEGER NOT NULL);
            CREATE UNIQUE INDEX IX_RegistrosImportados_UsuarioId_Chave ON RegistrosImportados(UsuarioId,Chave);
            """);
    }
    protected override void Down(MigrationBuilder m)
    {
        foreach(var tabela in new[]{"RegistrosImportados","OcorrenciasRecorrentes","Recorrencias","MembrosEspaco","EspacosFinanceiros","AcessosWeb"})
            m.DropTable(tabela);
    }
}
