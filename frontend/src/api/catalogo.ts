import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { UnidadeMedida, UnidadeMedidaFormValues, UnidadeMedidaResumo } from "@/features/catalogo/unidades-medida/types";

export const unidadesMedidaApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<UnidadeMedidaResumo>>(
      `/api/catalogo/unidades-medida?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<UnidadeMedida>(`/api/catalogo/unidades-medida/${id}`),
  create: (data: UnidadeMedidaFormValues) => http.post<Resultado<UnidadeMedida>>("/api/catalogo/unidades-medida", data),
  update: (id: number, data: UnidadeMedidaFormValues) => http.put<Resultado<UnidadeMedida>>(`/api/catalogo/unidades-medida/${id}`, data),
  delete: (id: number) => http.delete(`/api/catalogo/unidades-medida/${id}`),
};
