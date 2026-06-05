import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { MovimentacaoEstoque, MovimentacaoEstoqueFormValues, MovimentacaoEstoqueResumo } from "@/features/estoque/movimentacoes/types";

export const estoqueApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MovimentacaoEstoqueResumo>>(
      `/api/estoque/movimentacoes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<MovimentacaoEstoque>(`/api/estoque/movimentacoes/${id}`),
  create: (data: MovimentacaoEstoqueFormValues) => http.post<Resultado<MovimentacaoEstoque>>("/api/estoque/movimentacoes", data),
  update: (id: number, data: MovimentacaoEstoqueFormValues) => http.put<Resultado<MovimentacaoEstoque>>(`/api/estoque/movimentacoes/${id}`, data),
  delete: (id: number) => http.delete(`/api/estoque/movimentacoes/${id}`),
  confirmar: (id: number) => http.post<Resultado<MovimentacaoEstoque>>(`/api/estoque/movimentacoes/${id}/confirmar`, {}),
  cancelar: (id: number) => http.post<Resultado<MovimentacaoEstoque>>(`/api/estoque/movimentacoes/${id}/cancelar`, {}),
};
