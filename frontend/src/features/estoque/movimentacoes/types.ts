import { z } from "zod";
import type { Sku } from "@/features/catalogo/skus/types";

export interface MovimentacaoEstoqueItem {
  id: number;
  sku: Sku;
  produtoNome: string;
  unidadeMedidaSigla: string;
  quantidade: number;
  custoUnitario: number;
  quantidadeAnterior?: number | null;
  custoMedioAnterior?: number | null;
}

export interface MovimentacaoEstoque {
  id: number;
  dataMovimentacao: string;
  tipoMovimentacao: "ENTRADA" | "SAIDA" | "VENDA" | "BALANCO";
  status: "RASCUNHO" | "CONFIRMADA" | "CANCELADA";
  observacao?: string | null;
  usuario?: { id: number; nome: string } | null;
  nfeId?: number | null;
  vendaId?: number | null;
  movimentacoesEstoquesItens: MovimentacaoEstoqueItem[];
  valorTotal?: number; // Opcional, calculado ou enviado pelo backend
}

export const tipoMovimentacaoLabels: Record<string, string> = {
  ENTRADA: "Entrada",
  SAIDA: "Saída",
  VENDA: "Venda",
  BALANCO: "Balanço",
};

export const statusLabels: Record<string, string> = {
  RASCUNHO: "Rascunho",
  CONFIRMADA: "Efetivada",
  CANCELADA: "Estornada",
};

export const TIPOS_SEM_CUSTO = ["BALANCO"] as const;

export function tipoPrecisaDeCusto(tipo: string): boolean {
  return !TIPOS_SEM_CUSTO.includes(tipo as (typeof TIPOS_SEM_CUSTO)[number]);
}

export const movimentacaoEstoqueItemSchema = z.object({
  sku: z.string().min(1, "SKU é obrigatório."),
  quantidade: z
    .number({ invalid_type_error: "Deve ser um número." })
    .positive("Quantidade deve ser maior que zero."),
  custoUnitario: z
    .number({ invalid_type_error: "Deve ser um número." })
    .min(0, "Custo unitário não pode ser negativo.")
    .optional()
    .default(0),
});

export const movimentacaoEstoqueBaseSchema = z.object({
  tipoMovimentacao: z.enum(["ENTRADA", "SAIDA", "VENDA", "BALANCO"], {
    required_error: "Selecione o tipo de movimentação.",
  }),
  usuarioId: z.number().nullable().optional(),
  nfeId: z.number().nullable().optional(),
  vendaId: z.number().nullable().optional(),
  observacao: z
    .string()
    .max(500, "Observação deve ter no máximo 500 caracteres.")
    .nullable()
    .optional(),
  itens: z
    .array(movimentacaoEstoqueItemSchema)
    .min(1, "A movimentação deve conter pelo menos um produto."),
});

export const movimentacaoEstoqueSchema = movimentacaoEstoqueBaseSchema.refine(
  (data) => {
    if (data.tipoMovimentacao === "VENDA") {
      return data.vendaId !== null && data.vendaId !== undefined;
    }
    return true;
  },
  {
    message: "A venda correspondente é obrigatória para movimentação de venda.",
    path: ["vendaId"],
  },
);

export type MovimentacaoEstoqueItemFormValues = z.infer<
  typeof movimentacaoEstoqueItemSchema
>;
export type MovimentacaoEstoqueFormValues = z.infer<
  typeof movimentacaoEstoqueSchema
>;
