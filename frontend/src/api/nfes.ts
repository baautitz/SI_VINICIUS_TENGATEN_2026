import { http, API_URL } from "./http";
import type { PaginatedResult, Resultado } from "./types";
import type { Nfe, NfeResumo, StatusNfe, TipoOperacaoNfe } from "@/features/nfes/types";

export const nfesApi = {
  list: (p: { search?: string; tipo?: TipoOperacaoNfe; status?: StatusNfe; page: number; pageSize: number }) => {
    const q = new URLSearchParams({ page: String(p.page), pageSize: String(p.pageSize) });
    if (p.search) q.set("search", p.search);
    if (p.tipo) q.set("tipoOperacao", p.tipo);
    if (p.status) q.set("statusNfe", p.status);
    return http.get<PaginatedResult<NfeResumo>>(`/api/nfes?${q}`);
  },
  getById: (id: number) => http.get<Nfe>(`/api/nfes/${id}`),
  // O endpoint devolve application/xml, não JSON: não passa pelo http.
  xml: async (id: number) => {
    const res = await fetch(`${API_URL}/api/nfes/${id}/xml`);
    if (res.status === 404) throw new Error("Esta NF-e não tem XML guardado.");
    if (!res.ok) throw new Error("Não foi possível obter o XML da NF-e.");
    return res.text();
  },
  emitirDeVenda: (vendaId: number) => http.post<Resultado<Nfe>>(`/api/vendas/${vendaId}/nfe`, {}),
  cancelar: (id: number, justificativa: string) =>
    http.post<Resultado<Nfe>>(`/api/nfes/${id}/cancelar`, { justificativa }),
};
