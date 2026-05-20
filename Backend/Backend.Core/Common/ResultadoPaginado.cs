namespace Backend.Core.Common;

public record ResultadoPaginado<T>(
    IEnumerable<T> Itens,
    int TotalDeItens,
    int PaginaAtual,
    int TamanhoDaPagina
)
{
    public int TotalDePaginas => (int)Math.Ceiling(TotalDeItens / (double)TamanhoDaPagina);
    public bool TemPaginaAnterior => PaginaAtual > 1;
    public bool TemProximaPagina => PaginaAtual < TotalDePaginas;
}