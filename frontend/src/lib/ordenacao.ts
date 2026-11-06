export type DirecaoOrdenacao = "asc" | "desc";

/** Ordenação escolhida pelo usuário no cabeçalho; `null` significa a ordenação padrão da listagem. */
export interface Ordenacao {
  campo: string;
  direcao: DirecaoOrdenacao;
}

/** Ciclo do clique no cabeçalho: ascendente, descendente, padrão. */
export function proximaOrdenacao(atual: Ordenacao | null, campo: string): Ordenacao | null {
  if (!atual || atual.campo !== campo) return { campo, direcao: "asc" };
  if (atual.direcao === "asc") return { campo, direcao: "desc" };
  return null;
}

/** Trecho de query string (com `&` inicial) para anexar à URL da listagem. */
export function paramsDeOrdenacao(ordenacao: Ordenacao | null): string {
  if (!ordenacao) return "";
  return `&ordenarPor=${encodeURIComponent(ordenacao.campo)}&direcao=${ordenacao.direcao}`;
}
