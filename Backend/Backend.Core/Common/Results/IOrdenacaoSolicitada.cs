namespace Backend.Core.Common.Results;

/// <summary>
/// Ordenação pedida na requisição atual (query string <c>ordenarPor</c> e <c>direcao</c>).
/// Os repositórios de listagem a consultam e validam contra a lista branca da própria listagem.
/// </summary>
public interface IOrdenacaoSolicitada
{
    string? OrdenarPor { get; }
    string? Direcao { get; }
}
