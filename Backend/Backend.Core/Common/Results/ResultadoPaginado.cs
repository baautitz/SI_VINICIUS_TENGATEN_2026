namespace Backend.Core.Common.Results;

public sealed class ResultadoPaginado<T>
{
    public int Pagina { get; }
    public int TamanhoDaPagina { get; }
    public int TotalDeItens { get; }
    public int TotalDePaginas { get; }
    public IEnumerable<T> Itens { get; }

    public ResultadoPaginado(IEnumerable<T> itens, int totalDeItens, int pagina, int tamanhoDaPagina)
    {
        Itens = itens;
        TotalDeItens = totalDeItens;
        Pagina = pagina;
        TamanhoDaPagina = tamanhoDaPagina;
        TotalDePaginas = (int)Math.Ceiling(totalDeItens / (double)tamanhoDaPagina);
    }
}
