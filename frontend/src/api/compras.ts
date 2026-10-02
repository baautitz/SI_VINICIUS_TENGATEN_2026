import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type {
  Compra,
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
  previaNfe: (xml: string, emitenteId?: number) =>
    http.post<Resultado<PreviaNfe>>("/api/nfes/importar/previa", {
      xml,
      emitenteId: emitenteId ?? null,
    }),
  importarNfe: (data: ImportarNfePayload) =>
    http.post<Resultado<ImportarNfeResultado>>("/api/nfes/importar", data),
};
