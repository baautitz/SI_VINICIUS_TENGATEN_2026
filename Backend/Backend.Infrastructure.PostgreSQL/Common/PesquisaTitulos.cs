using System.Text.RegularExpressions;
using Backend.Core.Features.Financeiro.DTOs;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Common;

// Pesquisa fuzzy de títulos financeiros (contas a pagar/receber): cada palavra do termo precisa casar (sem acento/caixa)
// em qualquer campo do título, por substring, por forma normalizada (só dígitos de CPF/CNPJ, decimal com vírgula)
// ou por similaridade de trigramas (tolera erro de digitação em palavras com 4+ letras). O resultado é ordenado por relevância.
public static class PesquisaTitulos
{
    // Literais: a palavra escapada para LIKE (%, _ e \ valem como texto). Valores: o valor monetário que a palavra representa, se houver.
    public sealed record Termo(string[] Palavras, string[] Alternativas, string[] Literais, decimal?[] Valores);

    public static Termo Analisar(string? texto)
    {
        var palavras = (texto ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return new Termo(palavras, palavras.Select(Alternativa).ToArray(), palavras.Select(Literal).ToArray(), palavras.Select(Valor).ToArray());
    }

    private static string Literal(string palavra) => palavra.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // "1.100,00" -> 1100; "10.5" -> 10,5; "10" -> 10. Só valor inteiro: o valor não entra no texto pesquisável (trecho de dígitos seria ruído).
    private static decimal? Valor(string palavra)
    {
        if (Regex.IsMatch(palavra, @"^\d{1,3}(\.\d{3})+(,\d{1,2})?$") || Regex.IsMatch(palavra, @"^\d+(,\d{1,2})?$"))
            return decimal.Parse(palavra.Replace(".", "").Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
        if (Regex.IsMatch(palavra, @"^\d+\.\d{1,2}$"))
            return decimal.Parse(palavra, System.Globalization.CultureInfo.InvariantCulture);
        return null;
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
        if (Regex.IsMatch(palavra, @"^\d+\.\d{1,2}$") || Regex.IsMatch(palavra, @",\d{1,2}$")) return "";
        return Regex.Replace(palavra, @"\D", "");
    }

    // `texto`: coluna texto_pesquisa do título (já sem acento/caixa, mantida por trigger, com índice trigram). Uma cláusula por palavra,
    // cada uma indexável (LIKE, forma alternativa ou `<%`, que equivale a word_similarity >= 0.5 com o limiar de AjustarLimiar).
    public static string Where(string texto, Termo termo)
    {
        var sb = new System.Text.StringBuilder("\n            WHERE TRUE");
        for (var i = 0; i < termo.Palavras.Length; i++)
        {
            var ramos = new List<string> { $"{texto} LIKE '%' || unaccent(lower(@Literal{i})) || '%'" };
            if (termo.Alternativas[i] != "") ramos.Add($"{texto} LIKE '%' || @Alternativa{i} || '%'");
            if (termo.Valores[i] != null) ramos.Add($"{texto.Replace("texto_pesquisa", "valor")} = @Valor{i}");
            // Tolerância a erro de digitação só para palavra de 4+ letras sem curinga: número/data por trigrama casa tudo parecido.
            var difusa = termo.Palavras[i].Length >= 4 && termo.Palavras[i].Any(char.IsLetter) && termo.Palavras[i].IndexOfAny(['%', '_']) < 0;
            if (difusa) ramos.Add($"unaccent(lower(@Palavra{i})) <% {texto}");
            sb.Append($"\n              AND ({string.Join(" OR ", ramos)})");
        }
        return sb.ToString();
    }

    // O operador `<%` usa pg_trgm.word_similarity_threshold (padrão 0.6); a pesquisa sempre usou 0.5. Vale para a conexão.
    public static Task AjustarLimiar(System.Data.IDbConnection conexao, System.Data.IDbTransaction? transacao)
        => conexao.ExecuteAsync("SELECT set_config('pg_trgm.word_similarity_threshold', '0.5', false);", transaction: transacao);

    public static string Relevancia(string texto, string documento, string descricao) => $@"
            (SELECT COALESCE(SUM(
                CASE WHEN unaccent(lower({documento})) = unaccent(lower(t.palavra)) THEN 6 ELSE 0 END
                + CASE WHEN unaccent(lower({descricao})) = unaccent(lower(t.palavra)) THEN 3 ELSE 0 END
                + CASE WHEN unaccent(lower({descricao})) LIKE '%' || unaccent(lower(t.palavra)) || '%' THEN 2 ELSE 0 END
                + word_similarity(unaccent(lower(t.palavra)), {texto})), 0)
             FROM unnest(@Palavras) t(palavra)) DESC";

    // Parâmetros comuns da pesquisa paginada (termo, filtros comuns e paginação); cada repositório soma os seus.
    public static DynamicParameters Parametros(string? termo, IFiltroTitulos? filtro, int pagina, int tamanhoDaPagina)
    {
        var pesquisa = Analisar(termo);
        var p = new DynamicParameters();
        p.AddDynamicParams(new
        {
            pesquisa.Palavras,
            pesquisa.Alternativas,
            filtro?.ClienteId,
            OrigemTipo = filtro?.OrigemTipo?.ToString(),
            filtro?.OrigemId,
            filtro?.NumeroDocumento,
            Status = filtro?.Status?.ToString(),
            CpfCnpj = SomenteDigitos(filtro?.CpfCnpj),
            filtro?.VencimentoDe,
            filtro?.VencimentoAte,
            filtro?.EmissaoDe,
            filtro?.EmissaoAte,
            filtro?.ValorMinimo,
            filtro?.ValorMaximo,
            TamanhoDaPagina = tamanhoDaPagina,
            Offset = (pagina - 1) * tamanhoDaPagina
        });
        for (var i = 0; i < pesquisa.Palavras.Length; i++)
        {
            p.Add($"Palavra{i}", pesquisa.Palavras[i]);
            p.Add($"Alternativa{i}", pesquisa.Alternativas[i]);
            p.Add($"Literal{i}", pesquisa.Literais[i]);
            p.Add($"Valor{i}", pesquisa.Valores[i]);
        }
        return p;
    }

    // Cláusulas dos filtros comuns (menos CPF/CNPJ, que depende das contrapartes de cada tela); `t` é o alias da tabela do título.
    public static string FiltroComum(string t) => $@"
              AND (@ClienteId::int IS NULL OR {t}.cliente_id = @ClienteId)
              AND (@OrigemTipo::text IS NULL OR {t}.origem_tipo::text = @OrigemTipo)
              AND (@OrigemId::int IS NULL OR {t}.origem_id = @OrigemId)
              AND (@NumeroDocumento::text IS NULL OR {t}.numero_documento = @NumeroDocumento)
              AND (@Status::text IS NULL OR {t}.status::text = @Status)
              AND (@VencimentoDe::date IS NULL OR {t}.data_vencimento >= @VencimentoDe)
              AND (@VencimentoAte::date IS NULL OR {t}.data_vencimento <= @VencimentoAte)
              AND (@EmissaoDe::date IS NULL OR {t}.data_emissao >= @EmissaoDe)
              AND (@EmissaoAte::date IS NULL OR {t}.data_emissao <= @EmissaoAte)
              AND (@ValorMinimo::numeric IS NULL OR {t}.valor >= @ValorMinimo)
              AND (@ValorMaximo::numeric IS NULL OR {t}.valor <= @ValorMaximo)";
}
