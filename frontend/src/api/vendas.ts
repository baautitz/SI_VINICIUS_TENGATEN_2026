import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { CriarDevolucaoValues, Devolucao, Venda, VendaFormValues } from "@/features/vendas/types";

export const vendasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Venda>>(
      `/api/vendas?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  listByCliente: (clienteId: number, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Venda>>(
      `/api/vendas?clienteId=${clienteId}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Venda>(`/api/vendas/${id}`),
  create: (data: VendaFormValues) =>
    http.post<Resultado<Venda>>("/api/vendas", data),
  listDevolucoes: (vendaId: number) =>
    http.get<Devolucao[]>(`/api/vendas/${vendaId}/devolucoes`),
  createDevolucao: (vendaId: number, data: CriarDevolucaoValues) =>
    http.post<Resultado<Devolucao>>(`/api/vendas/${vendaId}/devolucoes`, data),
};
