import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Pais, PaisFormValues } from "@/features/localizacao/paises/types";
import type { Estado, EstadoFormValues } from "@/features/localizacao/estados/types";
import type { Cidade, CidadeFormValues } from "@/features/localizacao/cidades/types";
import type { Bairro, BairroFormValues } from "@/features/localizacao/bairros/types";

export const paisesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Pais>>(`/api/localizacao/paises?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Pais>(`/api/localizacao/paises/${id}`),
  create: (data: PaisFormValues) => http.post<Resultado<Pais>>("/api/localizacao/paises", data),
  update: (id: number, data: PaisFormValues) => http.put<Resultado<Pais>>(`/api/localizacao/paises/${id}`, data),
  delete: (id: number) => http.delete(`/api/localizacao/paises/${id}`),
};

export const estadosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Estado>>(`/api/localizacao/estados?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Estado>(`/api/localizacao/estados/${id}`),
  create: (data: EstadoFormValues) => http.post<Resultado<Estado>>("/api/localizacao/estados", data),
  update: (id: number, data: EstadoFormValues) => http.put<Resultado<Estado>>(`/api/localizacao/estados/${id}`, data),
  delete: (id: number) => http.delete(`/api/localizacao/estados/${id}`),
};

export const cidadesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Cidade>>(`/api/localizacao/cidades?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Cidade>(`/api/localizacao/cidades/${id}`),
  create: (data: CidadeFormValues) => http.post<Resultado<Cidade>>("/api/localizacao/cidades", data),
  update: (id: number, data: CidadeFormValues) => http.put<Resultado<Cidade>>(`/api/localizacao/cidades/${id}`, data),
  delete: (id: number) => http.delete(`/api/localizacao/cidades/${id}`),
};

export const bairrosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Bairro>>(`/api/localizacao/bairros?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Bairro>(`/api/localizacao/bairros/${id}`),
  create: (data: BairroFormValues) => http.post<Resultado<Bairro>>("/api/localizacao/bairros", data),
  update: (id: number, data: BairroFormValues) => http.put<Resultado<Bairro>>(`/api/localizacao/bairros/${id}`, data),
  delete: (id: number) => http.delete(`/api/localizacao/bairros/${id}`),
};
