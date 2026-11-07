using Backend.Core.Common.Results;

namespace Backend.Web.Middlewares;

/// <summary>Lê <c>ordenarPor</c> e <c>direcao</c> da query string da requisição atual.</summary>
public sealed class OrdenacaoDaRequisicao : IOrdenacaoSolicitada
{
    private readonly IHttpContextAccessor _http;

    public OrdenacaoDaRequisicao(IHttpContextAccessor http) => _http = http;

    public string? OrdenarPor => Ler("ordenarPor");
    public string? Direcao => Ler("direcao");

    private string? Ler(string chave)
    {
        var valor = _http.HttpContext?.Request.Query[chave].FirstOrDefault();
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
