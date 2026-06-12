import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { MetodoPagamento, MetodoPagamentoFormValues } from "@/features/pagamentos/metodos/types";
import type { CondicaoPagamento, CondicaoPagamentoFormValues } from "@/features/pagamentos/condicoes/types";

export const metodosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MetodoPagamento>>(`/api/pagamentos/metodos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (codigo: string) => http.get<MetodoPagamento>(`/api/pagamentos/metodos/${encodeURIComponent(codigo)}`),
  create: (data: MetodoPagamentoFormValues) => http.post<Resultado<MetodoPagamento>>("/api/pagamentos/metodos", data),
  update: (codigo: string, data: MetodoPagamentoFormValues) => http.put<Resultado<MetodoPagamento>>(`/api/pagamentos/metodos/${encodeURIComponent(codigo)}`, data),
  delete: (codigo: string) => http.delete(`/api/pagamentos/metodos/${encodeURIComponent(codigo)}`),
};

export const condicoesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<CondicaoPagamento>>(`/api/pagamentos/condicoes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<CondicaoPagamento>(`/api/pagamentos/condicoes/${id}`),
  create: (data: CondicaoPagamentoFormValues) => http.post<Resultado<CondicaoPagamento>>("/api/pagamentos/condicoes", data),
  update: (id: number, data: CondicaoPagamentoFormValues) => http.put<Resultado<CondicaoPagamento>>(`/api/pagamentos/condicoes/${id}`, data),
  delete: (id: number) => http.delete(`/api/pagamentos/condicoes/${id}`),
};
