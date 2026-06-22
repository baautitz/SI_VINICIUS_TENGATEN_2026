import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Cliente, ClienteFormValues } from "@/features/parceiros/clientes/types";
import type { Emitente, EmitenteFormValues } from "@/features/parceiros/emitentes/types";
import type { Fornecedor, FornecedorFormValues } from "@/features/parceiros/fornecedores/types";
import type { Transportadora, TransportadoraFormValues } from "@/features/parceiros/transportadoras/types";
import type { Veiculo, VeiculoFormValues } from "@/features/parceiros/veiculos/types";

export const clientesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Cliente>>(`/api/parceiros/clientes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Cliente>(`/api/parceiros/clientes/${id}`),
  create: (data: ClienteFormValues) => http.post<Resultado<Cliente>>("/api/parceiros/clientes", data),
  update: (id: number, data: ClienteFormValues) => http.put<Resultado<Cliente>>(`/api/parceiros/clientes/${id}`, data),
  delete: (id: number) => http.delete(`/api/parceiros/clientes/${id}`),
};

export const emitentesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Emitente>>(`/api/parceiros/emitentes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Emitente>(`/api/parceiros/emitentes/${id}`),
  create: (data: EmitenteFormValues) => http.post<Resultado<Emitente>>("/api/parceiros/emitentes", data),
  update: (id: number, data: EmitenteFormValues) => http.put<Resultado<Emitente>>(`/api/parceiros/emitentes/${id}`, data),
  delete: (id: number) => http.delete(`/api/parceiros/emitentes/${id}`),
};

export const fornecedoresApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Fornecedor>>(`/api/parceiros/fornecedores?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Fornecedor>(`/api/parceiros/fornecedores/${id}`),
  create: (data: FornecedorFormValues) => http.post<Resultado<Fornecedor>>("/api/parceiros/fornecedores", data),
  update: (id: number, data: FornecedorFormValues) => http.put<Resultado<Fornecedor>>(`/api/parceiros/fornecedores/${id}`, data),
  delete: (id: number) => http.delete(`/api/parceiros/fornecedores/${id}`),
};

export const transportadorasApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Transportadora>>(`/api/parceiros/transportadoras?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Transportadora>(`/api/parceiros/transportadoras/${id}`),
  create: (data: TransportadoraFormValues) => http.post<Resultado<Transportadora>>("/api/parceiros/transportadoras", data),
  update: (id: number, data: TransportadoraFormValues) => http.put<Resultado<Transportadora>>(`/api/parceiros/transportadoras/${id}`, data),
  delete: (id: number) => http.delete(`/api/parceiros/transportadoras/${id}`),
};

export const veiculosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<Veiculo>>(`/api/parceiros/veiculos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<Veiculo>(`/api/parceiros/veiculos/${id}`),
  create: (data: VeiculoFormValues) => http.post<Resultado<Veiculo>>("/api/parceiros/veiculos", data),
  update: (id: number, data: VeiculoFormValues) => http.put<Resultado<Veiculo>>(`/api/parceiros/veiculos/${id}`, data),
  delete: (id: number) => http.delete(`/api/parceiros/veiculos/${id}`),
};
