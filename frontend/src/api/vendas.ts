import { paramsDeOrdenacao, type Ordenacao } from "@/lib/ordenacao";
import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { CriarDevolucaoValues, Devolucao, DevolucaoResumo, Venda, VendaFormValues } from "@/features/vendas/types";

export const vendasApi = {
  list: (search?: string, page = 1, pageSize = 20, ordenacao: Ordenacao | null = null) =>
    http.get<PaginatedResult<Venda>>(
      `/api/vendas?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}${paramsDeOrdenacao(ordenacao)}`
    ),
  listByCliente: (clienteId: number, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Venda>>(
      `/api/vendas?clienteId=${clienteId}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Venda>(`/api/vendas/${id}`),
  create: (data: VendaFormValues) =>
    http.post<Resultado<Venda>>("/api/vendas", data),
  getDevolucaoById: (id: number) => http.get<Devolucao>(`/api/devolucoes/${id}`),
  searchDevolucoes: ({
    search,
    vendaId,
    page = 1,
    pageSize = 50,
    ordenacao = null,
  }: { search?: string; vendaId?: number; page?: number; pageSize?: number; ordenacao?: Ordenacao | null } = {}) =>
    http.get<PaginatedResult<DevolucaoResumo>>(
      `/api/devolucoes?search=${encodeURIComponent(search ?? "")}${
        vendaId ? `&vendaId=${vendaId}` : ""
      }&page=${page}&pageSize=${pageSize}${paramsDeOrdenacao(ordenacao)}`
    ),
  listDevolucoes: (vendaId: number) =>
    http.get<Devolucao[]>(`/api/vendas/${vendaId}/devolucoes`),
  estornarDevolucao: (id: number, motivo: string) =>
    http.post<Resultado<Devolucao>>(`/api/devolucoes/${id}/estornar`, { motivo }),
  createDevolucao: (vendaId: number, data: CriarDevolucaoValues) =>
    http.post<Resultado<Devolucao>>(`/api/vendas/${vendaId}/devolucoes`, data),
};
