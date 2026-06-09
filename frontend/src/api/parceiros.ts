import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Cliente, ClienteFormValues } from "@/features/parceiros/clientes/types";
import type { Emitente, EmitenteFormValues } from "@/features/parceiros/emitentes/types";
import type { Fornecedor, FornecedorFormValues } from "@/features/parceiros/fornecedores/types";

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
