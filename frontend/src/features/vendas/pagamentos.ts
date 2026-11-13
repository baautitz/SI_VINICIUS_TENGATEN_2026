// Calculos puros do checkout da venda: pagamentos (condicao + valor), restante, entrada e parcelas.
// Sem imports de alias para rodar direto no node nos testes.

export interface CondicaoParaPagamento {
  entradaMinimaPercentual: number;
  condicoesPagamentosParcelas?: { percentual: number; prazoDias: number }[];
}

export interface PagamentoLinha {
  condicao: CondicaoParaPagamento | null;
  valor: number;
}

export interface ParcelaGerada {
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
}

const arredondar = (v: number) => Math.round((v + Number.EPSILON) * 100) / 100;

export const ehAVista = (c: CondicaoParaPagamento | null | undefined) =>
  (c?.entradaMinimaPercentual ?? 0) >= 100;

export function somaPagamentos(linhas: PagamentoLinha[]): number {
  return arredondar(linhas.reduce((s, l) => s + (l.valor || 0), 0));
}

// Quanto falta cobrir do total (negativo = passou do total).
export function restante(total: number, linhas: PagamentoLinha[]): number {
  return arredondar(total - somaPagamentos(linhas));
}

// Exige condicao e valor > 0 em todas as linhas e restante zerado.
export function pagamentosCompletos(total: number, linhas: PagamentoLinha[]): boolean {
  return (
    total > 0 &&
    linhas.length > 0 &&
    linhas.every((l) => l.condicao !== null && l.valor > 0) &&
    Math.abs(restante(total, linhas)) < 0.01
  );
}

// Maior entrada minima (%) entre as condicoes a prazo usadas.
export function entradaMinimaExigida(linhas: PagamentoLinha[]): number {
  return Math.max(
    0,
    ...linhas.filter((l) => l.condicao && !ehAVista(l.condicao)).map((l) => l.condicao!.entradaMinimaPercentual),
  );
}

// Entrada minima das condicoes a prazo vale sobre a soma das linhas a vista. Devolve quanto falta (0 = ok).
export function entradaFaltante(total: number, linhas: PagamentoLinha[]): number {
  const aVista = linhas.filter((l) => ehAVista(l.condicao)).reduce((s, l) => s + (l.valor || 0), 0);
  return Math.max(0, arredondar((total * entradaMinimaExigida(linhas)) / 100 - aVista));
}

// Troco de uma linha em dinheiro: recebido acima do valor da linha.
export function trocoDaLinha(valor: number, recebido: number): number {
  return Math.max(0, arredondar(recebido - valor));
}

function somarDias(isoDate: string, dias: number): string {
  const d = new Date(isoDate + "T12:00:00");
  d.setDate(d.getDate() + dias);
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

// A vista: uma parcela na data da venda. A prazo: o valor da linha inteiro dividido pelos percentuais da condicao
// (normalizados para somar 100%); a diferenca de centavos vai na ultima parcela.
export function parcelasDaLinha(condicao: CondicaoParaPagamento, valor: number, dataVenda: string): ParcelaGerada[] {
  if (valor <= 0) return [];
  const prazos = condicao.condicoesPagamentosParcelas ?? [];
  if (ehAVista(condicao) || prazos.length === 0)
    return [{ numeroParcela: 1, dataVencimento: dataVenda, valorParcela: arredondar(valor) }];

  const somaPercentual = prazos.reduce((s, p) => s + p.percentual, 0);
  const parcelas = prazos.map((p, i) => ({
    numeroParcela: i + 1,
    dataVencimento: somarDias(dataVenda, p.prazoDias),
    valorParcela: arredondar((valor * p.percentual) / somaPercentual),
  }));
  const diferenca = valor - parcelas.reduce((s, p) => s + p.valorParcela, 0);
  parcelas[parcelas.length - 1].valorParcela = arredondar(parcelas[parcelas.length - 1].valorParcela + diferenca);
  return parcelas;
}
