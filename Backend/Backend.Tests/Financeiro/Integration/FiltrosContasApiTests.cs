using System.Net;
using System.Text.Json;
using Backend.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Backend.Tests.Financeiro.Integration;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class FiltrosContasApiTests
{
    private readonly HttpClient _http;

    public FiltrosContasApiTests(PostgresFixture db)
    {
        db.Limpar().GetAwaiter().GetResult();
        var factory = new WebApplicationFactory<Backend.Core.Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:DefaultConnection", db.ConnectionString));
        _http = factory.CreateClient();
    }

    private static async Task<JsonElement> Corpo(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    [Theory]
    [InlineData("contas-pagar")]
    [InlineData("contas-receber")]
    public async Task Listagem_sem_filtro_devolve_200(string rota)
    {
        var r = await _http.GetAsync($"/api/financeiro/{rota}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("contas-pagar")]
    [InlineData("contas-receber")]
    public async Task Listagem_com_todos_os_filtros_devolve_200(string rota)
    {
        var q = "status=ABERTO&origemTipo=MANUAL&origemId=1&numeroDocumento=X&cpfCnpj=123&vencimentoDe=2026-01-01&vencimentoAte=2026-12-31"
            + "&emissaoDe=2026-01-01&emissaoAte=2026-12-31&valorMinimo=1&valorMaximo=100&clienteId=1";

        var r = await _http.GetAsync($"/api/financeiro/{rota}?{q}");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    [Theory]
    [InlineData("contas-pagar", "status=NAO_EXISTE")]
    [InlineData("contas-receber", "status=NAO_EXISTE")]
    [InlineData("contas-pagar", "origemTipo=NAO_EXISTE")]
    [InlineData("contas-receber", "origemTipo=NAO_EXISTE")]
    [InlineData("contas-pagar", "vencimentoDe=abc")]
    [InlineData("contas-receber", "valorMinimo=abc")]
    public async Task Valor_invalido_devolve_400_no_padrao_de_erros(string rota, string query)
    {
        var r = await _http.GetAsync($"/api/financeiro/{rota}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var erro = (await Corpo(r)).GetProperty("errors")[0];
        Assert.Equal("VALIDATION_ERROR", erro.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("contas-pagar", "valorMinimo=50&valorMaximo=10", "ValorMinimo")]
    [InlineData("contas-receber", "valorMinimo=50&valorMaximo=10", "ValorMinimo")]
    [InlineData("contas-pagar", "vencimentoDe=2026-12-31&vencimentoAte=2026-01-01", "VencimentoDe")]
    [InlineData("contas-receber", "vencimentoDe=2026-12-31&vencimentoAte=2026-01-01", "VencimentoDe")]
    [InlineData("contas-pagar", "emissaoDe=2026-12-31&emissaoAte=2026-01-01", "EmissaoDe")]
    [InlineData("contas-receber", "emissaoDe=2026-12-31&emissaoAte=2026-01-01", "EmissaoDe")]
    public async Task Faixa_invertida_devolve_400_com_erro_de_validacao(string rota, string query, string campo)
    {
        var r = await _http.GetAsync($"/api/financeiro/{rota}?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var erro = (await Corpo(r)).GetProperty("errors")[0];
        Assert.Equal("VALIDATION_ERROR", erro.GetProperty("code").GetString());
        Assert.Equal(campo, erro.GetProperty("field").GetString());
    }
}
