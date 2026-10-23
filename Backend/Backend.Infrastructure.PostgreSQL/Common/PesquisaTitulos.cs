using System.Text.RegularExpressions;

namespace Backend.Infrastructure.PostgreSQL.Common;

// Pesquisa fuzzy de títulos financeiros (contas a pagar/receber): cada palavra do termo precisa casar (sem acento/caixa)
// em qualquer campo do título, por substring, por forma normalizada (só dígitos de CPF/CNPJ, decimal com vírgula)
// ou por similaridade de trigramas (tolera erro de digitação em palavras com 4+ letras). O resultado é ordenado por relevância.
public static class PesquisaTitulos
{
    public sealed record Termo(string[] Palavras, string[] Alternativas);

    public static Termo Analisar(string? texto)
    {
        var palavras = (texto ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return new Termo(palavras, palavras.Select(Alternativa).ToArray());
    }

    public static string? SomenteDigitos(string? valor)
    {
        var digitos = new string((valor ?? "").Where(char.IsDigit).ToArray());
        return digitos.Length == 0 ? null : digitos;
    }

    // "123.456.789-00" -> "12345678900"; "1.234,56" -> "1234.56". Vazio quando não há forma alternativa útil.
    private static string Alternativa(string palavra)
    {
        if (!Regex.IsMatch(palavra, @"^[\d.,/\-]+$")) return "";
        if (Regex.IsMatch(palavra, @"^\d+\.\d{1,2}$")) return "";
        if (Regex.IsMatch(palavra, @",\d{1,2}$")) return palavra.Replace(".", "").Replace(',', '.');
        return Regex.Replace(palavra, @"\D", "");
    }

    // `haystack`: expressão SQL com o texto pesquisável do título; `documento` e `descricao`: campos que pesam mais no ranking.
    public static string Where(string haystack) => $@"
            CROSS JOIN LATERAL (SELECT unaccent(lower({haystack})) AS texto) h
            WHERE NOT EXISTS (
                SELECT 1 FROM unnest(@Palavras, @Alternativas) t(palavra, alternativa)
                WHERE NOT (h.texto LIKE '%' || unaccent(lower(t.palavra)) || '%'
                           OR (t.alternativa <> '' AND h.texto LIKE '%' || t.alternativa || '%')
                           OR (length(t.palavra) >= 4 AND word_similarity(unaccent(lower(t.palavra)), h.texto) >= 0.5)))";

    public static string Relevancia(string documento, string descricao) => $@"
            (SELECT COALESCE(SUM(
                CASE WHEN unaccent(lower({documento})) = unaccent(lower(t.palavra)) THEN 6 ELSE 0 END
                + CASE WHEN unaccent(lower({descricao})) = unaccent(lower(t.palavra)) THEN 3 ELSE 0 END
                + CASE WHEN unaccent(lower({descricao})) LIKE '%' || unaccent(lower(t.palavra)) || '%' THEN 2 ELSE 0 END
                + word_similarity(unaccent(lower(t.palavra)), h.texto)), 0)
             FROM unnest(@Palavras) t(palavra)) DESC";
}
