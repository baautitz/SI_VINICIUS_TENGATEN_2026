import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Venda, VendaFormValues } from "@/features/vendas/types";

export const vendasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Venda>>(
      `/api/vendas?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<Venda>(`/api/vendas/${id}`),
  create: (data: VendaFormValues) =>
    http.post<Resultado<Venda>>("/api/vendas", data),
  cancel: (id: number, motivo: string) =>
    http.post<Resultado<void>>(`/api/vendas/${id}/cancelar`, { motivo }),
};
