import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { ContasPagar } from "@/features/financeiro/contas-pagar/types";
import type { ContasReceber } from "@/features/financeiro/contas-receber/types";
import type { MetodoPagamento, MetodoPagamentoFormValues } from "@/features/financeiro/metodos/types";
import type { CondicaoPagamento, CondicaoPagamentoFormValues } from "@/features/financeiro/condicoes/types";

export interface ParcelaPagarCommand {
  dataVencimento: string;
  valor: number;
}

export interface ContaPagarCreateValues {
  descricao: string;
  fornecedorId: number;
  numeroDocumento?: string | null;
  dataEmissao?: string | null;
  condicaoPagamentoId?: number | null;
  observacao?: string | null;
  parcelas: ParcelaPagarCommand[];
}

export interface ContaPagarUpdateValues {
  descricao: string;
  dataVencimento: string;
  valor: number;
  observacao?: string | null;
}

export interface ContaReceberCreateValues {
  descricao: string;
  clienteId: number;
  numeroDocumento?: string | null;
  dataEmissao?: string | null;
  condicaoPagamentoId?: number | null;
  observacao?: string | null;
  parcelas: ParcelaPagarCommand[];
}

export type ContaReceberUpdateValues = ContaPagarUpdateValues;

export type OrigemTituloFinanceiro = "MANUAL" | "VENDA" | "COMPRA" | "FRETE_COMPRA" | "DEVOLUCAO_VENDA";

const queryFiltro = (f: Record<string, string | number | undefined>) =>
  Object.entries(f)
    .filter(([, v]) => v !== undefined)
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`)
    .join("&");

export const contasPagarApi = {
  listByFornecedor: (fornecedorId: number, pageSize = 100) =>
    http.get<PaginatedResult<ContasPagar>>(`/api/financeiro/contas-pagar?${queryFiltro({ fornecedorId, pageSize })}`),
  listByCliente: (clienteId: number, pageSize = 100) =>
    http.get<PaginatedResult<ContasPagar>>(`/api/financeiro/contas-pagar?${queryFiltro({ clienteId, pageSize })}`),
  listByOrigem: (origemTipo: OrigemTituloFinanceiro, origemId: number, pageSize = 100) =>
    http.get<PaginatedResult<ContasPagar>>(`/api/financeiro/contas-pagar?${queryFiltro({ origemTipo, origemId, pageSize })}`),
  list: (
    f: { search?: string; status?: string; origemTipo?: string; origemId?: number; numeroDocumento?: string; cpfCnpj?: string; vencimentoDe?: string; vencimentoAte?: string; emissaoDe?: string; emissaoAte?: string; valorMinimo?: string; valorMaximo?: string; page?: number; pageSize?: number } = {},
  ) =>
    http.get<PaginatedResult<ContasPagar>>(
      `/api/financeiro/contas-pagar?${queryFiltro({ ...f, search: f.search ?? "", page: f.page ?? 1, pageSize: f.pageSize ?? 20 })}`,
    ),
  getById: (id: number) => http.get<ContasPagar>(`/api/financeiro/contas-pagar/${id}`),
  create: (data: ContaPagarCreateValues) =>
    http.post<Resultado<ContasPagar[]>>("/api/financeiro/contas-pagar", data),
  update: (id: number, data: ContaPagarUpdateValues) =>
    http.put<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}`, data),
  delete: (id: number) => http.delete(`/api/financeiro/contas-pagar/${id}`),
  registrarPagamento: (id: number, valor: number) =>
    http.post<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}/pagar`, { valor }),
  estornarPagamento: (id: number, valor: number) =>
    http.post<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}/estornar`, { valor }),
};

export const contasReceberApi = {
  listByCliente: (clienteId: number, pageSize = 100) =>
    http.get<PaginatedResult<ContasReceber>>(`/api/financeiro/contas-receber?${queryFiltro({ clienteId, pageSize })}`),
  listByOrigem: (origemTipo: OrigemTituloFinanceiro, origemId: number, pageSize = 100) =>
    http.get<PaginatedResult<ContasReceber>>(`/api/financeiro/contas-receber?${queryFiltro({ origemTipo, origemId, pageSize })}`),
  list: (
    f: { search?: string; status?: string; origemTipo?: string; origemId?: number; numeroDocumento?: string; cpfCnpj?: string; vencimentoDe?: string; vencimentoAte?: string; emissaoDe?: string; emissaoAte?: string; valorMinimo?: string; valorMaximo?: string; page?: number; pageSize?: number } = {},
  ) =>
    http.get<PaginatedResult<ContasReceber>>(
      `/api/financeiro/contas-receber?${queryFiltro({ ...f, search: f.search ?? "", page: f.page ?? 1, pageSize: f.pageSize ?? 20 })}`,
    ),
  getById: (id: number) => http.get<ContasReceber>(`/api/financeiro/contas-receber/${id}`),
  create: (data: ContaReceberCreateValues) =>
    http.post<Resultado<ContasReceber[]>>("/api/financeiro/contas-receber", data),
  update: (id: number, data: ContaReceberUpdateValues) =>
    http.put<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}`, data),
  delete: (id: number) => http.delete(`/api/financeiro/contas-receber/${id}`),
  registrarRecebimento: (id: number, valor: number) =>
    http.post<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}/receber`, { valor }),
  estornarRecebimento: (id: number, valor: number) =>
    http.post<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}/estornar`, { valor }),
};

export const metodosApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<MetodoPagamento>>(`/api/financeiro/metodos?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (codigo: string) => http.get<MetodoPagamento>(`/api/financeiro/metodos/${encodeURIComponent(codigo)}`),
  create: (data: MetodoPagamentoFormValues) => http.post<Resultado<MetodoPagamento>>("/api/financeiro/metodos", data),
  update: (codigo: string, data: MetodoPagamentoFormValues) => http.put<Resultado<MetodoPagamento>>(`/api/financeiro/metodos/${encodeURIComponent(codigo)}`, data),
  delete: (codigo: string) => http.delete(`/api/financeiro/metodos/${encodeURIComponent(codigo)}`),
};

export const condicoesApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<CondicaoPagamento>>(`/api/financeiro/condicoes?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`),
  getById: (id: number) => http.get<CondicaoPagamento>(`/api/financeiro/condicoes/${id}`),
  create: (data: CondicaoPagamentoFormValues) => http.post<Resultado<CondicaoPagamento>>("/api/financeiro/condicoes", data),
  update: (id: number, data: CondicaoPagamentoFormValues) => http.put<Resultado<CondicaoPagamento>>(`/api/financeiro/condicoes/${id}`, data),
  delete: (id: number) => http.delete(`/api/financeiro/condicoes/${id}`),
};
