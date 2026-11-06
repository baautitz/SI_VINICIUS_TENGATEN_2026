import type { ContasPagar } from "../contas-pagar/types";
import type { ContasReceber } from "../contas-receber/types";

// Identidade de um documento: origem + número + contraparte (a mesma unicidade que o banco impõe às parcelas).
// Usada para listar só as parcelas relacionadas de um título.
export interface DocumentoFiltro {
  numeroDocumento: string;
  origemTipo: string;
  origemId: number | null;
  fornecedorId: number | null;
  clienteId: number | null;
  transportadoraId: number | null;
}

export function documentoDeContaPagar(c: Pick<ContasPagar, "numeroDocumento" | "origemTipo" | "origemId" | "fornecedor" | "clienteId" | "transportadoraId">): DocumentoFiltro {
  return {
    numeroDocumento: c.numeroDocumento,
    origemTipo: c.origemTipo,
    origemId: c.origemId ?? null,
    fornecedorId: c.fornecedor?.id ?? null,
    clienteId: c.clienteId ?? null,
    transportadoraId: c.transportadoraId ?? null,
  };
}

export function documentoDeContaReceber(c: Pick<ContasReceber, "numeroDocumento" | "origemTipo" | "origemId" | "cliente">): DocumentoFiltro {
  return {
    numeroDocumento: c.numeroDocumento,
    origemTipo: c.origemTipo,
    origemId: c.origemId ?? null,
    fornecedorId: null,
    clienteId: c.cliente?.id ?? null,
    transportadoraId: null,
  };
}

export function contraparteParaApi(d: DocumentoFiltro | null) {
  return {
    fornecedorId: d?.fornecedorId ?? undefined,
    clienteId: d?.clienteId ?? undefined,
    transportadoraId: d?.transportadoraId ?? undefined,
  };
}
