import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Transportadora, TransportadoraFormValues } from "@/features/logistica/transportadoras/types";
import type { Veiculo, VeiculoFormValues } from "@/features/logistica/veiculos/types";

export const transportadorasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Transportadora>>(`/api/logistica/transportadoras?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Transportadora>(`/api/logistica/transportadoras/${id}`),
  create: (data: TransportadoraFormValues) => http.post<Resultado<Transportadora>>("/api/logistica/transportadoras", data),
  update: (id: number, data: TransportadoraFormValues) => http.put<Resultado<Transportadora>>(`/api/logistica/transportadoras/${id}`, data),
  delete: (id: number) => http.delete(`/api/logistica/transportadoras/${id}`),
};

export const veiculosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Veiculo>>(`/api/logistica/veiculos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Veiculo>(`/api/logistica/veiculos/${id}`),
  create: (data: VeiculoFormValues) => http.post<Resultado<Veiculo>>("/api/logistica/veiculos", data),
  update: (id: number, data: VeiculoFormValues) => http.put<Resultado<Veiculo>>(`/api/logistica/veiculos/${id}`, data),
  delete: (id: number) => http.delete(`/api/logistica/veiculos/${id}`),
};
