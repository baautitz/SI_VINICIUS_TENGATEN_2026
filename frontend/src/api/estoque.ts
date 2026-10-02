import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { MovimentacaoEstoque, MovimentacaoEstoqueFormValues } from "@/features/estoque/movimentacoes/types";
import type { Balanco } from "@/features/estoque/balancos/types";

export const estoqueApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MovimentacaoEstoque>>(
      `/api/estoque/movimentacoes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  listByOrigem: (origemTipo: string, origemId: number) =>
    http.get<PaginatedResult<MovimentacaoEstoque>>(
      `/api/estoque/movimentacoes?origemTipo=${origemTipo}&origemId=${origemId}`
    ),
  getById: (id: number) => http.get<MovimentacaoEstoque>(`/api/estoque/movimentacoes/${id}`),
  create: (data: MovimentacaoEstoqueFormValues) => http.post<Resultado<MovimentacaoEstoque>>("/api/estoque/movimentacoes", data),
  estornar: (id: number, motivo: string) => http.post<Resultado<MovimentacaoEstoque>>(`/api/estoque/movimentacoes/${id}/estornar`, { motivo }),
};

export interface BalancoPayload {
  observacao?: string | null;
  itens: { sku: string; quantidadeContada: number }[];
}

export const balancosApi = {
  list: (page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Balanco>>(`/api/estoque/balancos?page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Balanco>(`/api/estoque/balancos/${id}`),
  create: (data: BalancoPayload) => http.post<Resultado<Balanco>>("/api/estoque/balancos", data),
  update: (id: number, data: BalancoPayload) => http.put<Resultado<Balanco>>(`/api/estoque/balancos/${id}`, data),
  fechar: (id: number) => http.post<Resultado<Balanco>>(`/api/estoque/balancos/${id}/fechar`, {}),
  cancelar: (id: number, motivo?: string) =>
    http.post<Resultado<Balanco>>(`/api/estoque/balancos/${id}/cancelar`, { motivo: motivo ?? null }),
};
