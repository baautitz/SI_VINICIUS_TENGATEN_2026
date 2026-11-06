import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type {
  Compra,
  CriarDevolucaoCompraValues,
  DevolucaoCompra,
  CriarCompraPayload,
  ImportarNfePayload,
  ImportarNfeResultado,
  PreviaNfe,
} from "@/features/compras/types";

export const comprasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Compra>>(
      `/api/compras?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`,
    ),
  getById: (id: number) => http.get<Compra>(`/api/compras/${id}`),
  create: (data: CriarCompraPayload) =>
    http.post<Resultado<Compra>>("/api/compras", data),
  estornar: (id: number, motivo: string) =>
    http.postQuietly<Resultado<Compra>>(`/api/compras/${id}/estornar`, { motivo }),
  createDevolucao: (compraId: number, data: CriarDevolucaoCompraValues) =>
    http.post<Resultado<DevolucaoCompra>>(`/api/compras/${compraId}/devolucoes`, data),
  listDevolucoes: (compraId: number) =>
    http.get<DevolucaoCompra[]>(`/api/compras/${compraId}/devolucoes`),
  previaNfe: (xml: string, emitenteId?: number) =>
    http.post<Resultado<PreviaNfe>>("/api/nfes/importar/previa", {
      xml,
      emitenteId: emitenteId ?? null,
    }),
  importarNfe: (data: ImportarNfePayload) =>
    http.post<Resultado<ImportarNfeResultado>>("/api/nfes/importar", data),
};
