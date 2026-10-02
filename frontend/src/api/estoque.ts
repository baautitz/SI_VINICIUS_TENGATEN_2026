import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { MovimentacaoEstoque, MovimentacaoEstoqueFormValues } from "@/features/estoque/movimentacoes/types";
import type { Balanco } from "@/features/estoque/balancos/types";

export const estoqueApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MovimentacaoEstoque>>(
      `/api/estoque/movimentacoes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<MovimentacaoEstoque>(`/api/estoque/movimentacoes/${id}`),
  create: (data: MovimentacaoEstoqueFormValues) => http.post<Resultado<MovimentacaoEstoque>>("/api/estoque/movimentacoes", data),
  estornar: (id: number, motivo: string) => http.post<Resultado<MovimentacaoEstoque>>(`/api/estoque/movimentacoes/${id}/estornar`, { motivo }),
};

export const balancosApi = {
  list: (page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Balanco>>(`/api/estoque/balancos?page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Balanco>(`/api/estoque/balancos/${id}`),
  create: (data: { observacao?: string | null; skus?: string[] | null }) =>
    http.post<Resultado<Balanco>>("/api/estoque/balancos", data),
  informarContagem: (id: number, itens: { sku: string; quantidadeContada: number | null }[]) =>
    http.put<Resultado<Balanco>>(`/api/estoque/balancos/${id}/contagem`, { itens }),
  fechar: (id: number) => http.post<Resultado<Balanco>>(`/api/estoque/balancos/${id}/fechar`, {}),
  cancelar: (id: number) => http.post<Resultado<Balanco>>(`/api/estoque/balancos/${id}/cancelar`, {}),
};
