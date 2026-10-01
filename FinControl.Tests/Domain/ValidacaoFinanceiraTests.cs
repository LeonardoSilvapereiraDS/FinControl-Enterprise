using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;
namespace FinControl.Tests.Domain;

public sealed class ValidacaoFinanceiraTests
{
    [Fact]
    public void EnumInvalido_NaoPodeCriarCategoriaOuConta()
    {
        Assert.Throws<RegraDeNegocioException>(()=>new Categoria("Inválida",(TipoCategoria)99,1));
        Assert.Throws<RegraDeNegocioException>(()=>new Conta("Inválida",(TipoConta)123,0,1));
    }
    [Fact]
    public void ValoresComFracaoDeCentavo_SaoRejeitados()
    {
        Assert.Throws<RegraDeNegocioException>(()=>new Conta("Conta",TipoConta.ContaCorrente,0.001m,1));
        Assert.Throws<RegraDeNegocioException>(()=>new Transacao("Despesa",10.001m,DateTime.Today,TipoTransacao.Despesa,1,TipoCategoria.Despesa,1,1));
    }
    [Fact]
    public void SaldoNegativo_ValidoMasValorExtremoRejeitado()
    {
        Assert.Equal(-25.50m,new Conta("Cheque especial",TipoConta.ContaCorrente,-25.50m,1).SaldoInicial);
        Assert.Throws<RegraDeNegocioException>(()=>new Conta("Limite",TipoConta.ContaCorrente,decimal.MinValue,1));
    }
    [Fact]
    public void TransacaoInvalida_NaoAlteraDescricaoExistente()
    {
        var t=new Transacao("Original",10m,DateTime.Today,TipoTransacao.Despesa,1,TipoCategoria.Despesa,1,1);
        Assert.Throws<RegraDeNegocioException>(()=>t.Atualizar("Alterada",-1m,DateTime.Today,TipoTransacao.Despesa,1,TipoCategoria.Despesa,1,null,true,false));
        Assert.Equal("Original",t.Descricao);
        Assert.Equal(10m,t.Valor);
    }
    [Fact]
    public void DatasForaDoCalendario_NaoSaoAceitas()
    {
        Assert.Throws<RegraDeNegocioException>(()=>new Orcamento(1,TipoCategoria.Despesa,1,100m,1,10000));
        Assert.Throws<RegraDeNegocioException>(()=>new Transacao("Data inválida",10m,DateTime.MinValue,TipoTransacao.Despesa,1,TipoCategoria.Despesa,1,1));
    }
}
