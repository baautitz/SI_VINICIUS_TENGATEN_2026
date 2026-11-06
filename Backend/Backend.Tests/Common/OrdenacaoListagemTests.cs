using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Results;

namespace Backend.Tests;

public class OrdenacaoListagemTests
{
    private static readonly IReadOnlyDictionary<string, string> ListaBranca = new Dictionary<string, string>
    {
        ["dataAbertura"] = "b.data_abertura",
        ["status"] = "b.status",
    };

    [Fact]
    public void Sem_escolha_do_usuario_vale_a_ordenacao_padrao()
        => Assert.Null(OrdenacaoListagem.MontarOrderBy(null, null, ListaBranca, "b.id"));

    [Theory]
    [InlineData("dataAbertura", "asc", "b.data_abertura ASC, b.id ASC")]
    [InlineData("status", "desc", "b.status DESC, b.id DESC")]
    [InlineData("status", null, "b.status ASC, b.id ASC")]
    [InlineData("status", "DESC", "b.status DESC, b.id DESC")]
    public void Campo_da_lista_branca_vira_order_by_com_desempate_estavel(string campo, string? direcao, string esperado)
        => Assert.Equal(esperado, OrdenacaoListagem.MontarOrderBy(campo, direcao, ListaBranca, "b.id"));

    [Theory]
    [InlineData("senha")]
    [InlineData("id; DROP TABLE balancos")]
    [InlineData("b.status")]
    public void Campo_fora_da_lista_branca_e_recusado(string campo)
        => Assert.Throws<DomainException>(() => OrdenacaoListagem.MontarOrderBy(campo, "asc", ListaBranca, "b.id"));

    [Fact]
    public void Direcao_invalida_e_recusada()
        => Assert.Throws<DomainException>(() => OrdenacaoListagem.MontarOrderBy("status", "sideways", ListaBranca, "b.id"));
}
