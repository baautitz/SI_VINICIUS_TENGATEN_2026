export type StatusBalanco = "ABERTO" | "FECHADO" | "CANCELADO";

export interface BalancoItem {
  id: number;
  sku: string;
  produtoNome: string;
  unidadeMedidaSigla: string;
  quantidadeSistema: number;
  quantidadeContada?: number | null;
  diferenca?: number | null;
}

export interface Balanco {
  id: number;
  dataAbertura: string;
  dataFechamento?: string | null;
  status: StatusBalanco;
  observacao?: string | null;
  usuario?: { id: number; nome: string } | null;
  itens: BalancoItem[];
}

export const statusBalancoLabels: Record<StatusBalanco, string> = {
  ABERTO: "Aberto",
  FECHADO: "Fechado",
  CANCELADO: "Cancelado",
};
