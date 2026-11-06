using Backend.Core.Common.Exceptions;

namespace Backend.Core.Common.Results;

/// <summary>
/// Traduz a ordenação pedida pelo usuário numa cláusula ORDER BY segura: só campos da lista branca
/// da listagem, nunca texto vindo da requisição.
/// </summary>
public static class OrdenacaoListagem
{
    /// <summary>Devolve null quando o usuário não escolheu (vale a ordenação padrão da listagem).</summary>
    public static string? MontarOrderBy(
        string? ordenarPor, string? direcao, IReadOnlyDictionary<string, string> listaBranca, string desempate)
    {
        if (string.IsNullOrWhiteSpace(ordenarPor)) return null;

        if (!listaBranca.TryGetValue(ordenarPor, out var coluna))
            throw new DomainException($"Ordenação por '{ordenarPor}' não é permitida nesta listagem.");

        var sentido = (direcao ?? "asc").ToLowerInvariant() switch
        {
            "asc" => "ASC",
            "desc" => "DESC",
            _ => throw new DomainException($"Direção de ordenação '{direcao}' inválida. Use 'asc' ou 'desc'."),
        };

        return $"{coluna} {sentido}, {desempate} {sentido}";
    }
}
