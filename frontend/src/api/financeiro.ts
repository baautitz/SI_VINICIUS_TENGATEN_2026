import { http } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { ContasPagar } from "@/features/financeiro/contas-pagar/types";
import type { ContasReceber } from "@/features/financeiro/contas-receber/types";

export interface ParcelaPagarCommand {
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
  valorPago: number;
  status: string;
}

export interface ContaPagarFormValues {
  descricao: string;
  fornecedorId: number;
  nfeId?: number | null;
  dataEmissao?: string | null;
  dataVencimento?: string | null;
  valorOriginal: number;
  condicaoPagamentoId?: number | null;
  observacao?: string | null;
  parcelas: ParcelaPagarCommand[];
}

export interface ParcelaReceberCommand {
  numeroParcela: number;
  dataVencimento: string;
  valorParcela: number;
  valorRecebido: number;
  status: string;
}

export interface ContaReceberFormValues {
  descricao: string;
  clienteId: number;
  nfeId?: number | null;
  vendaId?: number | null;
  dataEmissao?: string | null;
  dataVencimento?: string | null;
  valorOriginal: number;
  condicaoPagamentoId?: number | null;
  observacao?: string | null;
  parcelas: ParcelaReceberCommand[];
}

export const contasPagarApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<ContasPagar>>(
      `/api/financeiro/contas-pagar?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<ContasPagar>(`/api/financeiro/contas-pagar/${id}`),
  create: (data: ContaPagarFormValues) =>
    http.post<Resultado<ContasPagar>>("/api/financeiro/contas-pagar", data),
  update: (id: number, data: ContaPagarFormValues) =>
    http.put<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}`, data),
  delete: (id: number) => http.delete(`/api/financeiro/contas-pagar/${id}`),
  registrarPagamento: (id: number, numeroParcela: number, valorPago: number) =>
    http.post<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}/parcelas/${numeroParcela}/pagar`, {
      numeroParcela,
      valorPago,
    }),
  estornarPagamento: (id: number, numeroParcela: number, valorEstorno: number) =>
    http.post<Resultado<ContasPagar>>(`/api/financeiro/contas-pagar/${id}/parcelas/${numeroParcela}/estornar`, {
      numeroParcela,
      valorEstorno,
    }),
};

export const contasReceberApi = {
  list: (search?: string, page = 1, pageSize = 20) =>
    http.get<PaginatedResult<ContasReceber>>(
      `/api/financeiro/contas-receber?search=${encodeURIComponent(search ?? "")}&page=${page}&pageSize=${pageSize}`
    ),
  getById: (id: number) => http.get<ContasReceber>(`/api/financeiro/contas-receber/${id}`),
  create: (data: ContaReceberFormValues) =>
    http.post<Resultado<ContasReceber>>("/api/financeiro/contas-receber", data),
  update: (id: number, data: ContaReceberFormValues) =>
    http.put<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}`, data),
  delete: (id: number) => http.delete(`/api/financeiro/contas-receber/${id}`),
  registrarRecebimento: (id: number, numeroParcela: number, valorRecebido: number) =>
    http.post<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}/parcelas/${numeroParcela}/receber`, {
      numeroParcela,
      valorRecebido,
    }),
  estornarRecebimento: (id: number, numeroParcela: number, valorEstorno: number) =>
    http.post<Resultado<ContasReceber>>(`/api/financeiro/contas-receber/${id}/parcelas/${numeroParcela}/estornar`, {
      numeroParcela,
      valorEstorno,
    }),
};
