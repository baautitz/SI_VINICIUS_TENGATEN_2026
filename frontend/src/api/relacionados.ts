import { http } from "./http";

export interface RelacionadoItem {
  id: number;
  descricao: string;
  detalhe?: string | null;
}

const get = (path: string) => http.get<RelacionadoItem[]>(`/api/relacionados/${path}`);

export const relacionadosApi = {
  contasReceberPorVenda: (id: number) => get(`contas-receber/venda/${id}`),
  movimentacoesPorVenda: (id: number) => get(`movimentacoes/venda/${id}`),
  contasReceberPorCliente: (id: number) => get(`contas-receber/cliente/${id}`),
  contasPagarPorFornecedor: (id: number) => get(`contas-pagar/fornecedor/${id}`),
  vendasPorCliente: (id: number) => get(`vendas/cliente/${id}`),
  produtosPor: (campo: "categoriaId" | "marcaId" | "unidadeMedidaId", id: number) =>
    get(`produtos/${campo}/${id}`),
  localizacaoFilhos: (pai: "pais" | "estado" | "cidade", id: number) =>
    get(`localizacao/${pai}/${id}`),
};
